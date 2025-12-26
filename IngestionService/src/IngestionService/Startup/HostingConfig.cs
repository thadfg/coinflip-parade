using Microsoft.AspNetCore.Server.Kestrel.Core;

public static class HostingConfig
{
    public static void ConfigureCustomKestrel(WebHostBuilderContext context, KestrelServerOptions options)
    {
        var config = context.Configuration;
        var httpPort = config.GetValue<int?>("Kestrel:HttpPort");

        Console.WriteLine($"[Startup] Kestrel HTTP port: {httpPort}");

        // Only listen on HTTP. Caddy handles HTTPS.
        options.ListenAnyIP(5283);
    }
}

