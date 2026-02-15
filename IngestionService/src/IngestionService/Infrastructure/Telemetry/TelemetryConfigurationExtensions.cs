using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace; 
using OpenTelemetry.Logs;
using OpenTelemetry;


public static class TelemetryConfigurationExtensions
{
    public static void AddCustomTelemetry(this WebApplicationBuilder builder, string[] meterNames, bool enableRuntimeInstrumentation = true)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService("ingestion");

        builder.Services.AddOpenTelemetry()

            .ConfigureResource(r => r.AddService(serviceName: "ingestion"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("ingestion")
                    .AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
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
