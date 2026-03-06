using LotusDharma.Infrastructure.Data;
using LotusDharma.Web.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

#if (UseAspire)
builder.AddServiceDefaults();
#endif
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    app.UseHsts();
}

app.UseSecurityHeaders();

#if (!UseAspire)
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
#endif

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // only use HTTPS redirection on Production
}
app.UseResponseCompression();
app.UseStaticFiles();

app.UseCors("AllowFrontend");

if (builder.Configuration.GetValue("RateLimiting:EnableRateLimiting", true))
{
    app.UseRateLimiter();
}

app.UseExceptionHandler(options => { });

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(options =>
    {
        options.Path = "/api/specification-webapi.json";
    });

    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/api";
        settings.DocumentPath = "/api/specification-webapi.json";
        settings.DocExpansion = "list";
        settings.DefaultModelsExpandDepth = 1;
    });
}

#if (!UseApiOnly)
app.MapRazorPages();

app.MapFallbackToFile("index.html");
#endif

#if (UseApiOnly)
app.Map("/", () => Results.Redirect("/api"));
#endif

#if (UseAspire)
app.MapDefaultEndpoints();
#endif
app.MapEndpoints();

app.Run();

public partial class Program { }
