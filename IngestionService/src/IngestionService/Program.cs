using IngestionService.Startup;
using IngestionService.Web.Features.ComicCsv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

    builder.AddDependencies();

    var app = builder.Build();

    app.Logger.LogInformation("=== Program.cs updated at {Time} ===", DateTime.UtcNow);

    // Health checks
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHealthChecks("/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live") });
    app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

    // Ingestion endpoints
    app.MapComicCsvIngestorEndpoints();

    // Prometheus metrics endpoint (stable)
    app.UseMetricServer("/custom-metrics");
    app.UseHttpMetrics();

    // OpenAPI
    app.UseOpenApi();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Fatal error: {ex}");
    Console.WriteLine(ex.ToString()); // includes message, stack trace, and inner exceptions
    throw;
}
