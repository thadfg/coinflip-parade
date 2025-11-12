using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersistenceService.Application.Interfaces;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Logging;
using PersistenceService.Infrastructure.Repositories;
using Confluent.Kafka;
using PersistenceService.Infrastructure.Kafka;

namespace PersistenceService.Startup;

/// <summary>
/// Configuration class for managing dependencies in the PersistenceService.
/// </summary>

public static class DependenciesConfig
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {

        var env = builder.Environment.EnvironmentName;
        var config = builder.Configuration;

        // Add DbContext with PostgreSQL provider
        builder.Services.AddDbContext<EventDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("EventDb")));

        builder.Services.AddDbContext<ComicCollectionDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("EventDb")));


        // Add other dependencies as needed
        builder.Services.AddScoped<IEventRepository, EventRepository>();
        builder.Services.AddScoped<IComicCollectionRepository, ComicCollectionRepository>();

        // Kafka producer registration for logging (singleton)
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
            // add additional producer settings here if needed (Acks, LingerMs, etc.)
        };

        builder.Services.AddSingleton(producerConfig);
        builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
        {
            var pc = sp.GetRequiredService<ProducerConfig>();
            return new ProducerBuilder<Null, string>(pc).Build();
        });

        // Wrapper sink that uses the injected IProducer<Null,string>
        builder.Services.AddSingleton<IKafkaLogSink, KafkaLogSink>();
        builder.Services.AddSingleton<IKafkaLogHelper, KafkaLogHelper>();
    }
}
