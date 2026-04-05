using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ValuationService.Infrastructure;

public interface IMcpClientWrapper
{
    Task<string> ExecuteResearch(string prompt);
}

public class McpClientWrapper : IMcpClientWrapper
{
    private readonly string _nodePath;
    private readonly string _mcpCommand;
    private readonly string[] _mcpArgs;

    public McpClientWrapper()
    {
        bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
        if (isLinux)
        {
            _nodePath = "/usr/bin";
            _mcpCommand = "npx";
            _mcpArgs = new[] { "-y", "@playwright/mcp@latest" };
        }
        else
        {
            _nodePath = @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Microsoft\VisualStudio\NodeJs";
            _mcpCommand = "npx";
            _mcpArgs = new[] { "-y", "@playwright/mcp@latest" };
        }
    }

    public async Task<string> ExecuteResearch(string prompt)
    {
        bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
        var startInfo = new ProcessStartInfo
        {
            FileName = isLinux ? "npx" : Path.Combine(_nodePath, "node.exe"),
            Arguments = isLinux 
                ? $"{_mcpCommand} {string.Join(" ", _mcpArgs)}"
                : $"{Path.Combine(_nodePath, "npx.cmd")} {_mcpCommand} {string.Join(" ", _mcpArgs)}",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        return await CallMcpTool(startInfo, prompt);
    }

    private async Task<string> CallMcpTool(ProcessStartInfo startInfo, string prompt)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await process.StandardInput.WriteLineAsync(prompt);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            return $"{{\"isError\": true, \"result\": \"{error.Replace("\"", "\\\"")}\"}}";
        }

        return output;
    }
}
