using System.Text;
using SharedLibrary.Kafka;

namespace IngestionService.Infrastructure.Logging
{
    // Lazy Kafka logger provider: does NOT resolve IKafkaProducer at startup.
    public sealed class KafkaLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IServiceProvider _services;
        private readonly string _topic;
        private IExternalScopeProvider? _scopeProvider;

        public KafkaLoggerProvider(IServiceProvider services, IConfiguration configuration)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _topic = configuration["Logging:Kafka:Topic"] ?? "logs";
        }

        public ILogger CreateLogger(string categoryName)
            => new KafkaLogger(categoryName, _services, _topic, () => _scopeProvider);

        public void Dispose() { }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            => _scopeProvider = scopeProvider;
    }

    internal sealed class KafkaLogger : ILogger
    {
        private readonly string _category;
        private readonly IServiceProvider _services;
        private readonly string _topic;
        private readonly Func<IExternalScopeProvider?> _scopeProviderGetter;

        // Lazy producer reference
        private IKafkaProducer? _producer;

        public KafkaLogger(string category, IServiceProvider services, string topic, Func<IExternalScopeProvider?> scopeProviderGetter)
        {
            _category = category;
            _services = services;
            _topic = topic;
            _scopeProviderGetter = scopeProviderGetter;
        }

        public IDisposable BeginScope<TState>(TState state)
            => _scopeProviderGetter()?.Push(state) ?? NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter == null) return;

            var message = formatter(state, exception);

            var payload = new
            {
                Timestamp = DateTime.UtcNow,
                Level = logLevel.ToString(),
                Category = _category,
                EventId = eventId.Id,
                Message = message,
                Exception = exception?.ToString(),
                Scope = GetScope()
            };

            _ = SendToKafkaAsync(payload);
        }

        private IKafkaProducer? GetProducer()
        {
            if (_producer != null)
                return _producer;

            // Resolve IKafkaProducer lazily
            _producer = _services.GetService<IKafkaProducer>();
            return _producer;
        }

        private async Task SendToKafkaAsync(object payload)
        {
            var producer = GetProducer();
            if (producer == null)
                return; // Kafka disabled or not registered

            const int maxAttempts = 3;
            int attempt = 0;

            while (true)
            {
                try
                {
                    await producer.ProduceAsync(_topic, Guid.NewGuid().ToString(), payload)
                                  .ConfigureAwait(false);
                    return;
                }
                catch
                {
                    attempt++;
                    if (attempt >= maxAttempts)
                        return;

                    try
                    {
                        await Task.Delay(100 * attempt).ConfigureAwait(false);
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }

        private string? GetScope()
        {
            var provider = _scopeProviderGetter();
            if (provider == null) return null;

            var sb = new StringBuilder();
            provider.ForEachScope<object?>((scope, state) =>
            {
                if (sb.Length > 0) sb.Append(" => ");
                sb.Append(scope?.ToString());
            }, null);

            return sb.Length == 0 ? null : sb.ToString();
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
