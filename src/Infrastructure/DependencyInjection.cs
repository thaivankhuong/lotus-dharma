using System.Text;
using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Constants;
using LotusDharma.Infrastructure.BackgroundJobs;
using LotusDharma.Infrastructure.Caching;
using LotusDharma.Infrastructure.Messaging;
using LotusDharma.Infrastructure.Search;
using LotusDharma.Infrastructure.Storage;
using LotusDharma.Infrastructure.Data;
using LotusDharma.Infrastructure.Data.Interceptors;
using LotusDharma.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("LotusDharmaDb");
        Guard.Against.Null(connectionString, message: "Connection string 'LotusDharmaDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseNetTopologySuite();
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

#if (UseAspire)
        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();
#endif

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<LotusDharma.Application.Common.Interfaces.IGoogleAuthService, LotusDharma.Infrastructure.Services.GoogleAuthService>();
        builder.Services.AddSingleton<IBlobStorageService, LocalBlobStorageService>();
        builder.Services.AddScoped<IEventBus, InProcessEventBus>();
        builder.Services.AddSingleton<ISearchService, DatabaseSearchService>();

        ConfigureJwtAuthentication(builder);

        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));

        AddRedisCacheServices(builder);

        // builder.Services.AddHostedService<OutboxProcessorJob>(); // Temporarily disabled
        builder.Services.AddHostedService<TokenCleanupJob>();
    }

    private static void ConfigureJwtAuthentication(IHostApplicationBuilder builder)
    {
        var jwtSecret = builder.Configuration["JwtSettings:Secret"];
        Guard.Against.NullOrWhiteSpace(jwtSecret, message: "JwtSettings:Secret is required. Use User Secrets or Key Vault.");

        var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "LotusDharma";
        var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "LotusDharma";

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        builder.Services.AddAuthorizationBuilder();
    }

    private static void AddRedisCacheServices(IHostApplicationBuilder builder)
    {
        builder.Services.Configure<CacheOptions>(
            builder.Configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = builder.Configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        var redisConnection = builder.Configuration.GetConnectionString("Redis");

        if (string.IsNullOrEmpty(redisConnection))
        {
            builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
            return;
        }

        try
        {
            var configurationOptions = ConfigurationOptions.Parse(redisConnection);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectTimeout = 5000;
            configurationOptions.SyncTimeout = 5000;
            configurationOptions.AsyncTimeout = 5000;
            configurationOptions.ConnectRetry = 3;

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            builder.Services.AddSingleton<ICacheService, RedisCacheService>();
        }
        catch (Exception)
        {
            builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
        }
    }
}
