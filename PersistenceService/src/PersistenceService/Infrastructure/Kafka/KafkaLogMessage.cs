using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Diagnostics;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PersistenceService.Infrastructure.Logging
{
    internal sealed class KafkaLogMessage
    {
        public DateTime TimestampUtc { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string? Exception { get; set; }
        public string Host { get; set; } = Environment.MachineName;
        public string Service { get; set; } = "PersistenceService";
        public EventId EventId { get; set; }

        // New: correlation / trace id (nullable when not available)
        public string? CorrelationId { get; set; }
    }

    public sealed class KafkaLoggerProvider : ILoggerProvider
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;
        private readonly Channel<KafkaLogMessage> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _background;
        private readonly int _batchSize;
        private readonly TimeSpan _flushInterval;
        private bool _disposed;

        public KafkaLoggerProvider(IProducer<Null, string> producer, IConfiguration config, int batchSize = 50, TimeSpan? flushInterval = null)
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _topic = config["Kafka:LogTopic"] ?? "service-logs";
            _batchSize = Math.Max(1, batchSize);
            _flushInterval = flushInterval ?? TimeSpan.FromSeconds(2);

            // Bounded channel to avoid unbounded memory growth, drop when full to avoid blocking app threads.
            _channel = Channel.CreateBounded<KafkaLogMessage>(new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

            _background = Task.Run(() => BackgroundLoopAsync(_cts.Token));
        }

        public ILogger CreateLogger(string categoryName) => new KafkaLogger(this, categoryName);

        internal bool TryEnqueue(KafkaLogMessage msg) => _channel.Writer.TryWrite(msg);

        private async Task BackgroundLoopAsync(CancellationToken token)
        {
            var reader = _channel.Reader;
            var batch = new List<KafkaLogMessage>(_batchSize);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Wait until something is available or cancel
                    if (!await reader.WaitToReadAsync(token).ConfigureAwait(false))
                        break;

                    // Drain up to batchSize
                    while (reader.TryRead(out var item))
                    {
                        batch.Add(item);
                        if (batch.Count >= _batchSize) break;
                    }

                    if (batch.Count > 0)
                    {
                        var tasks = new List<Task>(batch.Count);
                        foreach (var entry in batch)
                        {
                            var payload = JsonSerializer.Serialize(entry);
                            var msg = new Message<Null, string> { Value = payload };
                            // ProduceAsync already returns Task<DeliveryResult<Null,string>>; no AsTask() needed.
                            tasks.Add(_producer.ProduceAsync(_topic, msg, token));
                        }

                        try
                        {
                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Swallow; don't let logging background crash application. Consider a fallback sink here.
                        }

                        batch.Clear();
                    }

                    // Wait a little to allow accumulation of logs when queue is slow-moving
                    await Task.Delay(_flushInterval, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                // Background exceptions are not fatal to app; they could be logged to console if desired.
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            try
            {
                _background.Wait(TimeSpan.FromSeconds(5));
            }
            catch { /* ignore */ }

            try
            {
                // Attempt flush of any in-flight messages
                _producer.Flush(TimeSpan.FromSeconds(5));
            }
            catch { /* ignore */ }

            // Do NOT dispose the producer here — DI owns the shared producer lifetime.
            _cts.Dispose();
        }
    }

    internal sealed class KafkaLogger : ILogger
    {
        private readonly KafkaLoggerProvider _provider;
        private readonly string _category;

        public KafkaLogger(KafkaLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            if (formatter == null) return;

            var message = formatter(state, exception);

            // Prefer Activity trace id when available, fall back to Activity.Id, otherwise null.
            string? correlationId = null;
            var activity = Activity.Current;
            if (activity != null)
            {
                // Activity.TraceId is a struct; ToString yields the hex trace id.
                correlationId = activity.TraceId != default ? activity.TraceId.ToString() : activity.Id;
            }

            var entry = new KafkaLogMessage
            {
                TimestampUtc = DateTime.UtcNow,
                Level = logLevel,
                Category = _category,
                Message = message,
                Exception = exception?.ToString(),
                EventId = eventId,
                CorrelationId = correlationId
            };

            // Best-effort, non-blocking enqueue. Messages may be dropped if queue is full.
            _provider.TryEnqueue(entry);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}