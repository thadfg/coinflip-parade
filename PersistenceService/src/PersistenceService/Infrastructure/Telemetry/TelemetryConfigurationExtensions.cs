using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedLibrary.Constants;
using System.Diagnostics.Metrics;

namespace PersistenceService.Infrastructure.Telemetry;

public static class TelemetryConfigurationExtensions
{
    private static readonly Meter UptimeMeter = new("PersistenceService.Uptime", "1.0.0");
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public static void AddCustomTelemetry(this WebApplicationBuilder builder)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(builder.Environment.ApplicationName);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("PersistenceService.Uptime")
                    .AddMeter(MeterNames.ComicPersistence)
                    .AddMeter("PersistenceService.Kafka")
                    .AddPrometheusExporter()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri("http://otel-collector:4317"); 
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            })
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri("http://otel-collector:4317");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            });

        UptimeMeter.CreateObservableGauge("process_uptime_seconds", () =>
        {
            return new Measurement<double>(
                (DateTime.UtcNow - StartTime).TotalSeconds,
                new KeyValuePair<string, object?>("service_name", builder.Environment.ApplicationName),
                new KeyValuePair<string, object?>("env", builder.Environment.EnvironmentName));
        });
    }
}
