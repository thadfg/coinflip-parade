using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersistenceService.Startup;
using PersistenceService.Infrastructure.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Register all DI dependencies (DbContexts, Repositories, Kafka, Logging)
builder.AddDependencies();

// Add controllers
builder.Services.AddControllers();

// Add health checks
builder.Services.AddHealthChecks();

// Add OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register Kafka listener as a hosted background service
builder.Services.AddHostedService<KafkaComicListener>();

var app = builder.Build();

// Development-only OpenAPI UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map health endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// HTTPS redirection (optional)
app.UseHttpsRedirection();

app.Run();
