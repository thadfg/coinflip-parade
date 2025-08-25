using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

public static class TelemetryConfigurationExtensions
{
    public static void AddCustomTelemetry(this IServiceCollection services, string[] meterNames)
    {
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("IngestionService"));

                foreach (var meter in meterNames)
                {
                    metrics.AddMeter(meter);
                }

                metrics.AddRuntimeInstrumentation();
                metrics.AddPrometheusExporter(options =>
                {
                    options.ScrapeResponseCacheDurationMilliseconds = 0;
                });
            });
    }
}
