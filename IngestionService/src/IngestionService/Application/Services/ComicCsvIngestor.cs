using CsvHelper;
using Facet.Extensions;
using IngestionService.Domain.Models;
using SharedLibrary.Constants;
using SharedLibrary.Extensions;
using SharedLibrary.Facet;
using SharedLibrary.Kafka;
using SharedLibrary.Models;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace IngestionService.Application.Services;

public class ComicCsvIngestor
{
    private readonly IKafkaProducer _producer;

    private static readonly Meter Meter = new(MeterNames.ComicIngestion);

    private static readonly Counter<long> IngestionSuccess =
        Meter.CreateCounter<long>(
            "ingestion_success_total",
            description: "Successful ingestion records");

    private static readonly Counter<long> IngestionFailure =
        Meter.CreateCounter<long>(
            "ingestion_failure_total",
            description: "Failed ingestion records");

    private static readonly Histogram<double> IngestionDuration =
        Meter.CreateHistogram<double>(
            "ingestion_duration_seconds",
            unit: "s",
            description: "Ingestion duration in seconds");

    private static readonly DateTimeOffset ServiceStart = DateTimeOffset.UtcNow;

    // Expose read-only start time without exposing the field itself
    public static DateTimeOffset ServiceStartTime => ServiceStart;

    private static readonly ObservableGauge<double> ServiceUptime =
        Meter.CreateObservableGauge("service_uptime_seconds", () =>
        {
            var now = DateTimeOffset.UtcNow;
            return (now - ServiceStart).TotalSeconds;
        }, null, description: "Service uptime in seconds");

    private static readonly ObservableGauge<double> LastSuccessTimestamp =
    Meter.CreateObservableGauge("last_success_timestamp", () =>
    {
        return _lastSuccessTimestamp;
    });

    private static double _lastSuccessTimestamp = 0;




    // ActivitySource used to create producer spans for tracing
    private static readonly ActivitySource ActivitySource = new("IngestionService.ComicCsvIngestor");

    public ComicCsvIngestor(IKafkaProducer producer)
    {
        _producer = producer;
    }

    public async Task IngestAsync(string csvPath)
    {
        var importId = Guid.NewGuid();

        var importIdStr = importId.ToString();
        var service = "ComicCsvIngestorService";
        var trigger = "UserUpload";

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<ComicCsvRecord>().ToList();

        int successCount = 0;
        int failureCount = 0;
        var started = DateTimeOffset.UtcNow;

        foreach (var record in records)
        {
            // per-record correlation id
            var correlationId = Guid.NewGuid().ToString();

            if (!record.IsValid(out var validationError))
            {
                var deadLetter = new DeadLetterEnvelope<ComicCsvRecord>
                {
                    ImportId = importId.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Reason = $"Validation failed: {validationError}",
                    FailedPayload = record,
                    EventType = "Ingestion"
                };

                var key = $"dead|{importId}|{correlationId}";

                // start activity so traceparent is available to the producer
                using (var activity = ActivitySource.StartActivity("Ingest.Record.DeadLetter", ActivityKind.Producer))
                {
                    activity?.SetTag("import.id", importId.ToString());
                    activity?.AddBaggage("correlation-id", correlationId);
                    await _producer.ProduceAsync("comic-ingestion-dead-letter", key, deadLetter, correlationId);
                }

                failureCount++;

                var tags = new TagList
                {
                    { "import_id", importIdStr },
                    { "service", service },
                    { "trigger", trigger }
                };
                IngestionFailure.Add(1, tags);

                continue;
            }

            try
            {
                var comicEvent = record.ToFacet<ComicCsvRecord, ComicCsvRecordDto>();

                var publisherKey = record.PublisherName.Normalize("-");
                var seriesKey = record.SeriesName.Normalize("-");
                var key = $"{publisherKey}|{seriesKey}|{importId}|{correlationId}";

                var envelope = new KafkaEnvelope<ComicCsvRecordDto>
                {
                    ImportId = importId.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Payload = comicEvent
                };

                using (var activity = ActivitySource.StartActivity("Ingest.Record.Produce", ActivityKind.Producer))
                {
                    activity?.SetTag("import.id", importId.ToString());
                    activity?.SetTag("publisher", publisherKey);
                    activity?.AddBaggage("correlation-id", correlationId);

                    await _producer.ProduceAsync("comic-imported", key, envelope, correlationId);
                }

                successCount++;

                var tags = new TagList
                {
                    { "import_id", importIdStr },
                    { "service", service },
                    { "trigger", trigger }
                };
                IngestionSuccess.Add(1, tags);
            }
            catch (Exception ex)
            {
                var deadLetter = new DeadLetterEnvelope<ComicCsvRecord>
                {
                    ImportId = importId.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Reason = ex.Message,
                    FailedPayload = record,
                    EventType = "Ingestion"
                };

                var key = $"dead|{importId}|{correlationId}";

                using (var activity = ActivitySource.StartActivity("Ingest.Record.DeadLetterOnError", ActivityKind.Producer))
                {
                    activity?.SetTag("import.id", importId.ToString());
                    activity?.AddBaggage("correlation-id", correlationId);
                    await _producer.ProduceAsync("comic-ingestion-dead-letter", key, deadLetter, correlationId);
                }

                failureCount++;

                var tags = new TagList
                {
                    { "import_id", importIdStr },
                    { "service", service },
                    { "trigger", trigger }
                };
                IngestionFailure.Add(1, tags);
            }
        }

        var metrics = new BatchIngestionMetrics
        {
            ImportId = importId.ToString(),
            StartedAt = started,
            CompletedAt = DateTimeOffset.UtcNow,
            TotalRecords = records.Count,
            SuccessfulRecords = successCount,
            FailedRecords = failureCount,
            SourceSystem = "CsvImportService",
            TriggeredBy = "UserUpload"
        };

        await _producer.ProduceAsync("comic-ingestion-metrics", importId.ToString(), metrics);

        _lastSuccessTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Console.WriteLine($"[Metrics] Updated LastSuccessTimestamp to: {_lastSuccessTimestamp}");


        var completed = DateTimeOffset.UtcNow;
        var durationSeconds = (completed - started).TotalSeconds;

        var durationTags = new TagList
        {
            { "import_id", importIdStr },
            { "service", service },
            { "trigger", trigger }
        };
        IngestionDuration.Record(durationSeconds, durationTags);
    }
}
