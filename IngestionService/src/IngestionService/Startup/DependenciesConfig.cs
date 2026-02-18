using IngestionService.Application.Services;
using IngestionService.Infrastructure.Kafka;
using IngestionService.Infrastructure.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedLibrary.Constants;
using IngestionService.Infrastructure.Telemetry;

namespace IngestionService.Startup;

public static class DependenciesConfig
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        // 1. Setup Logging/Telemetry first so we catch startup events
        builder.AddCustomTelemetry(new[] { MeterNames.ComicIngestion });

        // Force the static constructor of the ingestor to run 
        // so the Meter is registered with OpenTelemetry immediately
        _ = ComicCsvIngestor.ServiceStartTime;

        var env = builder.Environment.EnvironmentName;

        //logger.LogInformation("[Startup] Environment: {Env}", env);

        // Application Services
        builder.Services.AddScoped<ComicCsvIngestor>();

        // Infrastructure Services
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        // Kafka (Binding is handled internally within AddKafka)
        //logger.LogInformation("[Startup] Registering Kafka for {Env}", env);
        builder.Services.AddKafka(builder.Configuration);

        // OpenAPI & HTTP Logging
        builder.Services.AddOpenApiServices();
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
        });
    }


}
