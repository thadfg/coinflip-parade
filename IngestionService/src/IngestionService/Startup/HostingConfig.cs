using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace IngestionService.Startup
{
    public static class HostingConfig
    {
        public static void ConfigureCustomKestrel(this IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureKestrel((context, options) =>
            {
                var config = context.Configuration.GetSection("Kestrel:Endpoints:Https:Certificate");
                var certPath = config.GetValue<string>("Path");
                var certPassword = config.GetValue<string>("Password");

                if (string.IsNullOrWhiteSpace(certPath) || string.IsNullOrWhiteSpace(certPassword))
                {
                    throw new InvalidOperationException("Certificate path or password is missing in configuration.");
                }


                var cert = new X509Certificate2(certPath, certPassword, X509KeyStorageFlags.EphemeralKeySet);

                options.ListenAnyIP(7086, listenOptions =>
                {
                    listenOptions.UseHttps(cert);
                });

                options.ListenAnyIP(5229); // HTTP fallback
            });
        }
    }
}
