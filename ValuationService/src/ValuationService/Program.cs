using ValuationService.Service;
using PersistenceService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ValuationService.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<ComicDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default") 
                           ?? "Host=localhost;Port=5432;Database=comicdb;Username=comicadmin;Password=comicpass";
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<ValuationControlService>();
builder.Services.AddSingleton<IMcpClientWrapper, McpClientWrapper>();
builder.Services.AddHostedService<ValuationBackgroundWorker>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ComicDbContext>();

// OpenTelemetry
var otelResourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("valuation-service");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(otelResourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("ValuationService")
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(otelResourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("ValuationService")
        .AddPrometheusExporter()
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
        }));

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();