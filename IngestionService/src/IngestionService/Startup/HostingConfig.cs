using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

public static class HostingConfig
{
    public static void ConfigureCustomKestrel(WebHostBuilderContext context, KestrelServerOptions options)
    {
        var config = context.Configuration;

        var httpsPort = config.GetValue<int>("Kestrel:Endpoints:Https:Port", 7443/*7086*/);
        var certPath = config["Kestrel:Endpoints:Https:Certificate:Path"];
        var certPassword = config["Kestrel:Endpoints:Https:Certificate:Password"]
                           ?? Environment.GetEnvironmentVariable("Kestrel__Endpoints__Https__Certificate__Password");

        EnsurePortAvailable(httpsPort, "HTTPS");

        if (string.IsNullOrWhiteSpace(certPath))
            throw new InvalidOperationException("Certificate path is not configured.");

        if (!File.Exists(certPath))
            throw new FileNotFoundException($"Certificate file not found at path: {certPath}");

        if (string.IsNullOrWhiteSpace(certPassword))
            throw new InvalidOperationException("Certificate password is missing.");

        var certBytes = File.ReadAllBytes(certPath);
        var certificate = X509CertificateLoader.LoadPkcs12(certBytes, certPassword);

        if (certificate.NotAfter < DateTime.UtcNow)
            throw new InvalidOperationException($"Certificate has expired on {certificate.NotAfter:u}");

        options.ListenAnyIP(httpsPort, listenOptions =>
        {
            listenOptions.UseHttps(certificate);
        });
    }

    private static void EnsurePortAvailable(int port, string label)
    {
        if (!IsPortAvailable(port))
        {
            DumpActiveListeners();
            throw new IOException($"[Startup] {label} port {port} is already in use. Aborting startup.");
        }
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static void DumpActiveListeners()
    {
        var ipProps = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = ipProps.GetActiveTcpListeners();

        Console.WriteLine("[Startup] Active TCP listeners:");
        foreach (var ep in listeners)
        {
            Console.WriteLine($"  - {ep.Address}:{ep.Port}");
        }
    }
}
