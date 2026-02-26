using OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using IngestionService.Application.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace; 
using OpenTelemetry.Logs;
using OpenTelemetry;

namespace IngestionService.Infrastructure.Telemetry;
public static class TelemetryConfigurationExtensions
{
    private static readonly Meter UptimeMeter = new Meter("ComicIngestion.Meter");
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public static void AddCustomTelemetry(this WebApplicationBuilder builder, string[] meterNames, bool enableRuntimeInstrumentation = true)
    {
        
        var serviceName = builder.Configuration["OTEL_SETTINGS:ServiceName"] ?? builder.Environment.ApplicationName;
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName);

        // Allow configuring the OTLP endpoint via configuration or environment.
        // Config keys checked (in order): "OTEL_SETTINGS:OtlpEndpoint", "OTEL_EXPORTER_OTLP_ENDPOINT".
        var otlpEndpoint = builder.Configuration["OTEL_SETTINGS:OtlpEndpoint"]
            ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? "http://otel-collector:4317";

        builder.Services.AddOpenTelemetry()
            
            .WithTracing(tracing =>
            {
                tracing
                    .ConfigureResource(r => r.AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("IngestionService.ComicCsvIngestor")
                    .AddOtlpExporter(opt => { opt.Endpoint = new Uri(otlpEndpoint); });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()        
                    .AddHttpClientInstrumentation()            
                    .AddMeter(meterNames)
                    .AddMeter("ComicIngestion.Meter")
                    .AddReader(new PeriodicExportingMetricReader(new OtlpMetricExporter(new OtlpExporterOptions { Endpoint = new Uri(otlpEndpoint) }), 1000));

                
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
            options.AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(otlpEndpoint);
            });
        });
        
        UptimeMeter.CreateObservableGauge("service_uptime_seconds", () =>
        {
            return new Measurement<double>(
                (DateTime.UtcNow - StartTime).TotalSeconds,
                new KeyValuePair<string, object?>("service_name", serviceName),
                new KeyValuePair<string, object?>("env", builder.Environment.EnvironmentName)
            );
        });                
    }
}
