using CsvHelper;
using Facet.Extensions;
using IngestionService.Domain.Models;
using Prometheus;
using SharedLibrary.Extensions;
using SharedLibrary.Facet;
using SharedLibrary.Kafka;
using SharedLibrary.Models;
using System.Diagnostics;
using System.Globalization;

namespace IngestionService.Application.Services;

public class ComicCsvIngestor
{
    private readonly IKafkaProducer _producer;    
    private static readonly Counter IngestionSuccess =
        Metrics.CreateCounter(
            "ingestion_success_total",
            "Successful ingestion records",
            new CounterConfiguration
            {
                LabelNames = new[] { "import_id", "service", "trigger" }
            });

    private static readonly Counter IngestionFailure =
        Metrics.CreateCounter(
            "ingestion_failure_total",
            "Failed ingestion records",
            new CounterConfiguration
            {
                LabelNames = new[] { "import_id", "service", "trigger" }
            });

    private static readonly Histogram IngestionDuration =
        Metrics.CreateHistogram(
            "ingestion_duration_seconds",
            "Ingestion duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "import_id", "service", "trigger" },
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });



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
                IngestionFailure.WithLabels(importIdStr, service, trigger).Inc();

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
                IngestionSuccess.WithLabels(importIdStr, service, trigger).Inc();
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
                IngestionFailure.WithLabels(importIdStr, service, trigger).Inc();

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

        var completed = DateTimeOffset.UtcNow;
        var durationSeconds = (completed - started).TotalSeconds;

        IngestionDuration
            .WithLabels(importId.ToString(), "ComicCsvIngestorService", "UserUpload")
            .Observe(durationSeconds);

    }
}
