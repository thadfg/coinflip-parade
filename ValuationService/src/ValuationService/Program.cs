using ValuationService.Service;
using PersistenceService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ValuationService.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<McpOptions>(builder.Configuration.GetSection(McpOptions.Mcp));
builder.Services.Configure<OpenTelemetryOptions>(builder.Configuration.GetSection(OpenTelemetryOptions.OpenTelemetry));
builder.Services.Configure<ValuationService.Infrastructure.ScalarOptions>(builder.Configuration.GetSection(ValuationService.Infrastructure.ScalarOptions.Scalar));
builder.Services.Configure<ValuationOptions>(builder.Configuration.GetSection(ValuationOptions.Valuations));
builder.Services.Configure<ParserOptions>(builder.Configuration.GetSection(ParserOptions.Parser));

builder.Services.AddDbContext<ComicDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<ValuationControlService>();
builder.Services.AddSingleton<IValuationResponseParser, ValuationResponseParser>();
builder.Services.AddSingleton<IMcpClientWrapper, McpClientWrapper>();
builder.Services.AddHostedService<ValuationBackgroundWorker>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ComicDbContext>();

// OpenTelemetry
var otelOptions = builder.Configuration.GetSection(OpenTelemetryOptions.OpenTelemetry).Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
var otelResourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(otelOptions.ServiceName);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(otelResourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("ValuationService")
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(otelOptions.OtlpEndpoint);
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
            opt.Endpoint = new Uri(otelOptions.OtlpEndpoint);
        }));

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Container"))
{
    var scalarOptions = builder.Configuration.GetSection(ValuationService.Infrastructure.ScalarOptions.Scalar).Get<ValuationService.Infrastructure.ScalarOptions>() ?? new ValuationService.Infrastructure.ScalarOptions();
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar", options =>
    {
        options.Title = scalarOptions.Title;
        if (Enum.TryParse<ScalarTheme>(scalarOptions.Theme, true, out var theme))
            options.Theme = theme;
        if (Enum.TryParse<ScalarLayout>(scalarOptions.Layout, true, out var layout))
            options.Layout = layout;
        options.HideClientButton = true;
        options.Servers = [new ScalarServer(scalarOptions.ServerUrl)];
    });
}
else
{
    app.MapOpenApi();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();