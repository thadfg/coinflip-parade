using Microsoft.Extensions.Options;
using SharedLibrary.Kafka;


namespace IngestionService.Infrastructure.Kafka;

public static class KafkaConfiguration
{
    public static void AddKafka(this IServiceCollection services, IConfiguration config)
    {
        // Register KafkaSettings using the options pattern
        services.Configure<KafkaSettings>(config.GetSection("Kafka"));

        // Register KafkaProducer using the resolved settings
        services.AddSingleton<IKafkaProducer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            return new KafkaProducer(settings);
        });        
    }
}
