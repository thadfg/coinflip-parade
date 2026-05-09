namespace ValuationService.Infrastructure;

public class ParserOptions
{
    public const string Parser = "Parser";

    public decimal[] IgnoredValues { get; set; } = [2.0m, 4.0m];
    public decimal MaxValidValue { get; set; } = 1000000m;
    public string[] IgnoredLogMarkers { get; set; } = ["Executed DbCommand", "Parameters=["];
}
