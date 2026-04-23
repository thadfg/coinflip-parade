using IngestionService.Startup;
using IngestionService.Web.Features.ComicCsv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

    builder.AddDependencies();

    var app = builder.Build();

    app.Logger.LogInformation("[Startup] Environment: {Env}", app.Environment.EnvironmentName);

    app.Logger.LogInformation("=== Program.cs updated at {Time} ===", DateTime.UtcNow);

    // Health checks
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHealthChecks("/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live") });
    app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

    // Ingestion endpoints
    app.MapComicCsvIngestorEndpoints();

    // Metrics are exported via OTLP to the OpenTelemetry Collector; do not expose a Prometheus scrape endpoint here.

    // Prometheus scrape endpoint removed: metrics are exported via OTLP to the OpenTelemetry Collector

    // OpenAPI
    app.UseOpenApi();

    // Http logging middleware
    app.UseHttpsRedirection();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Fatal error: {ex}");
    Console.WriteLine(ex.ToString()); // includes message, stack trace, and inner exceptions
    throw;
}

public partial class Program { }
