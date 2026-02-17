using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace; 
using OpenTelemetry.Logs;
using OpenTelemetry;

namespace IngestionService.Infrastructure.Telemetry;
public static class TelemetryConfigurationExtensions
{
    public static void AddCustomTelemetry(this WebApplicationBuilder builder, string[] meterNames, bool enableRuntimeInstrumentation = true)
    {
        
        var serviceName = builder.Configuration["OTEL_SETTINGS:ServiceName"] 
                          ?? builder.Environment.ApplicationName;
        
        // Define the resource configuration once
        var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService(serviceName: serviceName);

        builder.Services.AddOpenTelemetry()
            
            .WithTracing(tracing =>
            {
                tracing
                    .ConfigureResource(r => r.AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("IngestionService.ComicCsvIngestor")
                    .AddOtlpExporter(opt => {
                        opt.Endpoint = new Uri("http://jaeger:4317"); // Matches your docker-compose
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()        
                    .AddHttpClientInstrumentation()            
                    .AddMeter(meterNames)
                    .AddPrometheusExporter()
                    .AddOtlpExporter();

                
                if (enableRuntimeInstrumentation)
                {
                    // This is the extension method from the .Runtime package
                    metrics.AddRuntimeInstrumentation();
                }
            });
        // This part is crucial—it's what finally replaces your KafkaLoggerProvider
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);            
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.IncludeFormattedMessage = true;
            options.AddOtlpExporter();
        });
    }
}
