using IngestionService.Startup;
using IngestionService.Web.Features.ComicCsv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

    builder.AddDependencies();

    var app = builder.Build();

    app.Logger.LogInformation("=== Program.cs updated at {Time} ===", DateTime.UtcNow);    

    // Map health check endpoint
    app.MapHealthChecks("/health").AllowAnonymous(); // keep this on HTTP


    app.MapHealthChecks("/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });

    app.MapHealthChecks("/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });


    app.MapComicCsvIngestorEndpoints();

    app.UseOpenTelemetryPrometheusScrapingEndpoint("/custom-metrics"); // Default: /metrics

    //OpenApiConfig.UseOpenApi(app);
    app.UseOpenApi();


    app.Run();
}
catch (Exception ex)
{

    // This will show up in `docker logs ingestion`
    Console.WriteLine($"[Startup] Fatal error: {ex}");
    Console.WriteLine(ex.ToString()); // includes message, stack trace, and inner exceptions
    throw; // rethrow so the container exits instead of hanging
}



