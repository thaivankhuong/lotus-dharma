using Azure.Identity;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Infrastructure.Data;
using LotusDharma.Web.Services;
using Microsoft.AspNetCore.Mvc;
using NSwag;
using NSwag.Generation.Processors.Security;

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
            .AddDbContextCheck<ApplicationDbContext>();
#endif

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();

#if (!UseApiOnly)
        builder.Services.AddRazorPages();
#endif

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "🪷 Lotus Dharma API";
            configure.Description = @"
                        ## Authentication với JWT Bearer Token
                        POST /api/identity/login
                        {
                          ""email"": ""admin@example.com"",
                          ""password"": ""Admin123!""
                        }
                        ";

            // Thêm JWT Bearer Authentication cho Swagger
            configure.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Nhập JWT Bearer token"
            });

            // Thêm security requirement cho tất cả endpoints (trừ AllowAnonymous)
            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        });
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
