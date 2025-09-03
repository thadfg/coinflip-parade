using Microsoft.Extensions.Options;
using SharedLibrary.Kafka;

namespace IngestionService.Infrastructure.Kafka;

public static class KafkaConfiguration
{
    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind and validate KafkaSettings
        services.AddOptions<KafkaSettings>()
            .Bind(configuration.GetSection("Kafka"))
            .ValidateDataAnnotations()
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.BootstrapServers),
                "Kafka BootstrapServers must be configured.");

        // Register KafkaProducer
        services.AddSingleton<IKafkaProducer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaSettings>>();
            var logger = sp.GetRequiredService<ILogger<KafkaProducer>>();
            return new KafkaProducer(options, logger);
        });

        return services;
    }
}
