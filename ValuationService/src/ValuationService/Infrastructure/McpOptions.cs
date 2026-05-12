namespace ValuationService.Infrastructure;

public class McpOptions
{
    public const string Mcp = "Mcp";

    public string NodePath { get; set; } = string.Empty;
    public string McpCommand { get; set; } = "npx";
    public string[] McpArgs { get; set; } = [];

    // OS Specific values
    public string WindowsNodePath { get; set; } = string.Empty;
    public string LinuxNodePath { get; set; } = string.Empty;
    public string WindowsNpxCommand { get; set; } = "npx.cmd";
    public string LinuxNpxCommand { get; set; } = "npx";
    public string WindowsNodeExecutable { get; set; } = "node.exe";
    public string LinuxNodeExecutable { get; set; } = "node";
}
