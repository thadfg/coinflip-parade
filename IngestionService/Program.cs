using IngestionService.Configuration;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using SharedLibrary.Constants;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
TelemetryConfiguration.ConfigureOpenTelemetry(builder.Services, new[] { MeterNames.ComicIngestion });


var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint("/custom-metrics"); // Default: /metrics


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.Run();

