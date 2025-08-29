using IngestionService.Startup;
using IngestionService.Web.Features.ComicCsv;



var builder = WebApplication.CreateBuilder(args);
builder.AddDependencies();

var app = builder.Build();


app.UseHttpsRedirection();

// Map health check endpoint
app.MapHealthChecks("/health");

app.MapComicCsvIngestorEndpoints();

app.UseOpenTelemetryPrometheusScrapingEndpoint("/custom-metrics"); // Default: /metrics

//OpenApiConfig.UseOpenApi(app);
app.UseOpenApi();


app.Run();

