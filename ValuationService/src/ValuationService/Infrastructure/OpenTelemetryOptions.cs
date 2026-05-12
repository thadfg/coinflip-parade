namespace ValuationService.Infrastructure;

public class OpenTelemetryOptions
{
    public const string OpenTelemetry = "OpenTelemetry";

    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public string ServiceName { get; set; } = "valuation-service";
}
