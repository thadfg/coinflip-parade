using Microsoft.AspNetCore.Server.Kestrel.Core;

public static class HostingConfig
{
    public static void ConfigureCustomKestrel(WebHostBuilderContext context, KestrelServerOptions options)
    {
        var config = context.Configuration;        
        var url = config.GetValue<string>("Kestrel:Endpoints:Http:Url");

        Console.WriteLine($"[Startup] Kestrel HTTP port: {url}");

        // Only listen on HTTP. Caddy handles HTTPS.
        options.ListenAnyIP(new Uri(url).Port);
    }
}

