using IngestionService.Application.Services;
using IngestionService.Infrastructure.Kafka;
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
        // Disable runtime instrumentation to avoid OpenMetrics spec violations in Prometheus
        builder.Services.AddCustomTelemetry(new[] { MeterNames.ComicIngestion }, enableRuntimeInstrumentation: true);
        builder.Services.AddOpenApiServices();
        builder.WebHost.ConfigureCustomKestrel();        

        // Other dependencies can be registered here as needed
    }
}
