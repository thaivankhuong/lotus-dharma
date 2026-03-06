using System.Threading.RateLimiting;
using Azure.Identity;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Infrastructure.Data;
using LotusDharma.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSwag;
using NSwag.Generation.Processors.Security;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();
#if (!UseAspire)
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready" });

        AddRedisHealthCheck(builder);
#endif

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();

#if (!UseApiOnly)
        builder.Services.AddRazorPages();
#endif

        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        AddCorsPolicy(builder);
        AddRateLimiting(builder);
        AddSwagger(builder);

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        builder.Services.AddEndpointsApiExplorer();
    }

    private static void AddCorsPolicy(IHostApplicationBuilder builder)
    {
        var allowedOrigins = builder.Configuration
            .GetSection("CorsSettings:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                          .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With")
                          .AllowCredentials();
                }
            });
        });
    }

    private static void AddRateLimiting(IHostApplicationBuilder builder)
    {
        var enabled = builder.Configuration.GetValue("RateLimiting:EnableRateLimiting", true);
        if (!enabled) return;

        var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
        var queueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 10);
        var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 5);
        var loginWindowSeconds = builder.Configuration.GetValue("RateLimiting:LoginWindowSeconds", 300);
        var publicPermitLimit = builder.Configuration.GetValue("RateLimiting:PublicPermitLimit", 200);
        var publicWindowSeconds = builder.Configuration.GetValue("RateLimiting:PublicWindowSeconds", 60);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddSlidingWindowLimiter("default", limiterOptions =>
            {
                limiterOptions.PermitLimit = permitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(windowSeconds);
                limiterOptions.SegmentsPerWindow = 4;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = queueLimit;
            });

            options.AddFixedWindowLimiter("login", limiterOptions =>
            {
                limiterOptions.PermitLimit = loginPermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(loginWindowSeconds);
                limiterOptions.QueueLimit = 0;
            });

            options.AddSlidingWindowLimiter("public", limiterOptions =>
            {
                limiterOptions.PermitLimit = publicPermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(publicWindowSeconds);
                limiterOptions.SegmentsPerWindow = 4;
                limiterOptions.QueueLimit = 5;
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetSlidingWindowLimiter(remoteIp, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit * 2,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    SegmentsPerWindow = 4,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = queueLimit
                });
            });
        });
    }

    private static void AddSwagger(IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "Lotus Dharma API";
            configure.Description = "REST API for LotusDharma platform. Authenticate via POST /api/identity/login.";

            configure.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Input JWT Bearer token"
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        });
    }

    private static void AddRedisHealthCheck(IHostApplicationBuilder builder)
    {
        var redisConnection = builder.Configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            builder.Services.AddHealthChecks()
                .AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });
        }
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}
