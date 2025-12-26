using IngestionService.Application.Services;
using IngestionService.Infrastructure.Kafka;
using IngestionService.Infrastructure.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedLibrary.Constants;



namespace IngestionService.Startup;

public static class DependenciesConfig
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        var env = builder.Environment.EnvironmentName;
        var config = builder.Configuration;
        var kafkaConfig = new Confluent.Kafka.ProducerConfig();
        config.GetSection("Kafka").Bind(kafkaConfig);

        // Log active environment
        Console.WriteLine($"[Startup] Environment: {env}");        

        // Application Services
        builder.Services.AddScoped<ComicCsvIngestor>();

        // Infrastructure Services
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
            //.AddKafka(kafkaConfig, tags: new[] { "ready" });


        // Kafka setup
        Console.WriteLine($"[Startup] Registering Kafka for {env}");
        builder.Services.AddKafka(config);

        // Register Kafka-based logger provider (will send Error+ logs to Kafka)
       builder.Services.AddSingleton<ILoggerProvider, KafkaLoggerProvider>();

        // Telemetry
        builder.Services.AddCustomTelemetry(
            new[] { MeterNames.ComicIngestion },
            enableRuntimeInstrumentation: false
        );

        // OpenAPI
        builder.Services.AddOpenApiServices();
      

        // Kestrel config
        builder.WebHost.ConfigureKestrel(HostingConfig.ConfigureCustomKestrel);

    }

}
