using IngestionService.Application.Services;
using IngestionService.Infrastructure.Kafka;
using SharedLibrary.Constants;

namespace IngestionService.Startup;

public static class DependenciesConfig
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {
        var env = builder.Environment.EnvironmentName;
        var config = builder.Configuration;

        // Log active environment
        Console.WriteLine($"[Startup] Environment: {env}");

        // Log configured ports
        var httpPort = config.GetValue<int?>("Kestrel:HttpPort") ?? 5283;
        var httpsPort = config.GetValue<int?>("Kestrel:HttpsPort") ?? 7086;
        Console.WriteLine($"[Startup] Configured ports: HTTP {httpPort}, HTTPS {httpsPort}");

        // Application Services
        builder.Services.AddScoped<ComicCsvIngestor>();

        // Infrastructure Services
        builder.Services.AddHealthChecks();

        // Kafka setup
        Console.WriteLine($"[Startup] Registering Kafka for {env}");
        builder.Services.AddKafka(config);

        // Telemetry
        builder.Services.AddCustomTelemetry(
            new[] { MeterNames.ComicIngestion },
            enableRuntimeInstrumentation: false
        );

        // OpenAPI
        builder.Services.AddOpenApiServices();

        // HTTPS cert validation
        ValidateHttpsCert(config);

        // Kestrel config
        builder.WebHost.ConfigureKestrel(HostingConfig.ConfigureCustomKestrel);

        // Other dependencies can be registered here as needed
    }

    private static void ValidateHttpsCert(IConfiguration config)
    {
        var certSection = config.GetSection("Kestrel:Endpoints:Https:Certificate");
        var certPath = certSection.GetValue<string>("Path");
        var certPassword = config["Kestrel:Endpoints:Https:Certificate:Password"]
                           ?? Environment.GetEnvironmentVariable("Kestrel__Endpoints__Https__Certificate__Password");

        if (string.IsNullOrWhiteSpace(certPath) || !File.Exists(certPath))
        {
            Console.WriteLine($"[Startup] WARNING: HTTPS certificate not found at path: {certPath}");
        }

        if (string.IsNullOrWhiteSpace(certPassword))
        {
            Console.WriteLine("[Startup] WARNING: HTTPS certificate password is missing");
        }
    }
}
