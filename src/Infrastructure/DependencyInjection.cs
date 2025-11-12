using System.Text;
using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Constants;
using LotusDharma.Infrastructure.Caching;
using LotusDharma.Infrastructure.Data;
using LotusDharma.Infrastructure.Data.Interceptors;
using LotusDharma.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
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
            // Sử dụng PostgreSQL
            options.UseNpgsql(connectionString);
        });

#if (UseAspire)
        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();
#endif

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        // Thêm Custom Identity Services
        builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        // Google authentication service
        builder.Services.AddScoped<LotusDharma.Application.Common.Interfaces.IGoogleAuthService, LotusDharma.Infrastructure.Services.GoogleAuthService>();

        // Cấu hình JWT Authentication
        var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
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
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        builder.Services.AddAuthorizationBuilder();

        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));

        // Redis Distributed Cache
        AddRedisCacheServices(builder);
    }

    private static void AddRedisCacheServices(IHostApplicationBuilder builder)
    {
        // Configure cache options
        builder.Services.Configure<CacheOptions>(
            builder.Configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = builder.Configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        // Get Redis connection string
        var redisConnection = builder.Configuration.GetConnectionString("Redis");

        if (string.IsNullOrEmpty(redisConnection))
        {
            // No Redis configured - use in-memory cache as fallback
            builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
            
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine("⚠️  Redis not configured. Using in-memory cache (not suitable for production!)");
            }
            
            return;
        }

        try
        {
            // Configure Redis connection
            var configurationOptions = ConfigurationOptions.Parse(redisConnection);
            configurationOptions.AbortOnConnectFail = false; // Graceful handling of connection failures
            configurationOptions.ConnectTimeout = 5000;
            configurationOptions.SyncTimeout = 5000;
            configurationOptions.AsyncTimeout = 5000;
            configurationOptions.ConnectRetry = 3;

            // Register Redis connection
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var connection = ConnectionMultiplexer.Connect(configurationOptions);
                
                // Log connection events
                connection.ConnectionFailed += (sender, args) =>
                {
                    Console.WriteLine($"❌ Redis connection failed: {args.Exception?.Message}");
                };
                
                connection.ConnectionRestored += (sender, args) =>
                {
                    Console.WriteLine("✅ Redis connection restored");
                };

                if (builder.Environment.IsDevelopment())
                {
                    Console.WriteLine($"✅ Redis connected: {configurationOptions.EndPoints.First()}");
                }

                return connection;
            });

            // Register cache service
            builder.Services.AddSingleton<ICacheService, RedisCacheService>();

            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine($"✅ Redis cache enabled with compression: {cacheOptions.EnableCompression}");
            }
        }
        catch (Exception ex)
        {
            // Fallback to in-memory cache if Redis fails to initialize
            builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
            
            Console.WriteLine($"⚠️  Redis initialization failed: {ex.Message}. Using in-memory cache.");
        }
    }
}
