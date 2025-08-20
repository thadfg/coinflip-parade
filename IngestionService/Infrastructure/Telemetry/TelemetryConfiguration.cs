using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace IngestionService.Configuration;

public static class TelemetryConfiguration
{
    public static void ConfigureOpenTelemetry(IServiceCollection services, string[] meters) //support multiple meters or toggle exporters
    {
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                foreach (var meter in meters)
                {
                    metrics.AddMeter(meter);
                }

                metrics.AddPrometheusExporter();
                metrics.AddRuntimeInstrumentation();
            });
    }
}
