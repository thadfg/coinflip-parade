using IngestionService.Application.Services;
using IngestionService.Infrastructure.Kafka;
using IngestionService.Configuration;
using SharedLibrary.Constants;

namespace IngestionService.Startup;

public static class DependenciesConfig
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        // Application Services
        builder.Services.AddScoped<ComicCsvIngestor>();
        // Infrastructure Services
        builder.Services.AddHealthChecks();
        builder.Services.AddKafka(builder.Configuration);
        builder.Services.AddCustomTelemetry(new[] { MeterNames.ComicIngestion });
        builder.Services.AddOpenApiServices();
        TelemetryConfiguration.ConfigureOpenTelemetry(builder.Services, new[] { MeterNames.ComicIngestion });

        // Other dependencies can be registered here as needed
    }
}
