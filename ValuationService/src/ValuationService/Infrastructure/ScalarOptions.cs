namespace ValuationService.Infrastructure;

public class ScalarOptions
{
    public const string Scalar = "Scalar";

    public string ServerUrl { get; set; } = "https://localhost:8443";
    public string Title { get; set; } = "Valuation Service API";
    public string Theme { get; set; } = "Saturn";
    public string Layout { get; set; } = "Modern";
}
