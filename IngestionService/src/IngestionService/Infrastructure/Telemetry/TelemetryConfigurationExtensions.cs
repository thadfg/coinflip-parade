using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;


public static class TelemetryConfigurationExtensions
{
    public static void AddCustomTelemetry(this IServiceCollection services, string[] meterNames, bool enableRuntimeInstrumentation = true)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: "ingestion"))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()                    
                    .AddMeter(meterNames)
                    .AddPrometheusExporter();

                // NOTE: enableRuntimeInstrumentation is intentionally not wired up here unless
                // you add the corresponding runtime instrumentation package.
                _ = enableRuntimeInstrumentation;
            });
    }
}
