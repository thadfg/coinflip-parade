using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

public static class TelemetryConfigurationExtensions
{
    public static void AddCustomTelemetry(this IServiceCollection services, string[] meterNames, bool enableRuntimeInstrumentation = true)
    {
        // No OpenTelemetry Metrics here anymore.
        // You can keep this method if you want to register your own custom meters later,
        // but for now it does nothing.
    }
}
