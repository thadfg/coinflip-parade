using Scalar.AspNetCore;
namespace IngestionService.Startup;

public static class OpenApiConfig
{
    public static void AddOpenApiServices(this IServiceCollection services)
    {
        services.AddOpenApi();
    }

    public static void UseOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Container"))
        {
            app.MapOpenApi();
            
            var serverUrls =
                app.Configuration.GetSection("OpenApi:Servers").Get<string[]>()
                ?? ["https://localhost:8443"];
            
            app.MapScalarApiReference(options =>
            {
                options.Title = "Comic Ingestion API";
                options.Theme = ScalarTheme.Saturn;
                options.Layout = ScalarLayout.Modern;
                options.HideClientButton = true;
                options.Servers = serverUrls.Select(url => new ScalarServer(url)).ToList();
            });
        }
    }
}
