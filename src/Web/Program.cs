using LotusDharma.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if (UseAspire)
builder.AddServiceDefaults();
#endif
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

#if (!UseAspire)
app.UseHealthChecks("/health");
#endif
//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseExceptionHandler(options => { });

// Enable authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();


// Serve OpenAPI/Swagger document
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
// DEBUG: In ra tất cả endpoint groups
Console.WriteLine("=== REGISTERED ENDPOINT GROUPS ===");
var assembly = typeof(Program).Assembly;
var endpointGroupType = typeof(EndpointGroupBase);
var groups = assembly.GetExportedTypes()
    .Where(t => t.IsSubclassOf(endpointGroupType))
    .Select(t => t.Name)
    .ToList();

foreach (var group in groups)
{
    Console.WriteLine($"  ✓ {group}");
}
Console.WriteLine("==================================");

app.Run();

public partial class Program { }
