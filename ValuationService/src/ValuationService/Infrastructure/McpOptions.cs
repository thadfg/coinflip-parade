namespace ValuationService.Infrastructure;

public class McpOptions
{
    public const string Mcp = "Mcp";

    public string NodePath { get; set; } = string.Empty;
    public string McpCommand { get; set; } = "npx";
    public string[] McpArgs { get; set; } = [];
}
