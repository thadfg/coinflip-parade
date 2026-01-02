using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Database;
using PersistenceService.Infrastructure.Kafka;
using PersistenceService.Startup;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Register all DI dependencies (DbContexts, Repositories, Kafka, Logging)
builder.AddDependencies();

builder.Services.AddControllers();

// Add health checks
builder.Services.AddHealthChecks();

// Add OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register Kafka listener as a hosted background service
builder.Services.AddHostedService<KafkaComicListener>();

var app = builder.Build();

// Run DB readiness / migrations before Kafka listener starts
DbInitializer.Initialize(app.Services);


// Development-only OpenAPI UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map health endpoint
app.MapHealthChecks("/health");

app.MapHealthEndpoints();

app.UseMetricServer("/metrics");


// Map controllers
app.MapControllers();

// HTTPS redirection (optional)
app.UseHttpsRedirection();

app.Run();
