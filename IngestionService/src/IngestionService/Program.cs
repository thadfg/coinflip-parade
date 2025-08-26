using IngestionService.Application.Services;
using IngestionService.Configuration;
using IngestionService.Infrastructure.Kafka;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using SharedLibrary.Constants;


var builder = WebApplication.CreateBuilder(args);

// Register health checks
builder.Services.AddHealthChecks();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
TelemetryConfiguration.ConfigureOpenTelemetry(builder.Services, new[] { MeterNames.ComicIngestion });

builder.Services.AddKafka(builder.Configuration);

builder.Services.AddScoped<ComicCsvIngestor>();


// Register telemetry
builder.Services.AddCustomTelemetry(new[] { MeterNames.ComicIngestion });

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health");

app.MapComicCsvIngestorEndpoints();

app.UseOpenTelemetryPrometheusScrapingEndpoint("/custom-metrics"); // Default: /metrics





// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.Run();

