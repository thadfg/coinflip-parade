using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersistenceService.Application.Interfaces;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Logging;
using PersistenceService.Infrastructure.Repositories;
using Confluent.Kafka;
using PersistenceService.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using PersistenceService.Infrastructure.Database;

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
            options.UseNpgsql(config.GetConnectionString("EventDb"), npgsqlOptions => 
            { 
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public"); 
            }));

        builder.Services.AddDbContext<ComicCollectionDbContext>(options =>            
            options.UseNpgsql(config.GetConnectionString("EventDb"), npgsqlOptions => 
            { 
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public"); 
            }));


        // Add other dependencies as needed
        builder.Services.AddScoped<IDatabaseReadyChecker, DatabaseReadyChecker>();
        builder.Services.AddScoped<IEventRepository, EventRepository>();
        builder.Services.AddScoped<IComicCollectionRepository, ComicCollectionRepository>();

        // Kafka producer registration for logging (singleton)
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
            // add additional producer settings here if needed (Acks, LingerMs, etc.)
        };

        // Create the producer instance now and register it as the single shared instance.
        var sharedProducer = new ProducerBuilder<Null, string>(producerConfig).Build();
        builder.Services.AddSingleton<IProducer<Null, string>>(sharedProducer);

        // Wrapper sink and helper that use the injected IProducer<Null,string>
        builder.Services.AddSingleton<IKafkaLogSink, KafkaLogSink>();
        builder.Services.AddSingleton<IKafkaLogHelper, KafkaLogHelper>();

        // Create the KafkaLoggerProvider with the same shared producer and add to logging pipeline.
        var kafkaLoggerProvider = new KafkaLoggerProvider(sharedProducer, config);
        builder.Logging.AddProvider(kafkaLoggerProvider);

        // Register provider for disposal when DI container is disposed (so it will be disposed with the app).
        builder.Services.AddSingleton<ILoggerProvider>(kafkaLoggerProvider);
    }
}
