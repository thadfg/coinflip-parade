using IngestionService.Configuration;
using OpenTelemetry;
using OpenTelemetry.Metrics;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
TelemetryConfiguration.ConfigureOpenTelemetry(builder.Services, new[] { "ComicIngestionMetrics" });


var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint(); // Default: /metrics


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.Run();

