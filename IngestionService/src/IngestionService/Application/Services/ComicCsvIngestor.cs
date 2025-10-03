using CsvHelper;
using Facet.Extensions;
using IngestionService.Domain.Models;
using SharedLibrary.Extensions;
using SharedLibrary.Kafka;
using System.Diagnostics.Metrics;
using System.Globalization;
using SharedLibrary.Constants;
using SharedLibrary.Models;
using SharedLibrary.Facet;


namespace IngestionService.Application.Services;

public class ComicCsvIngestor
{
    private readonly IKafkaProducer _producer;
    private static readonly Meter Meter = new(MeterNames.ComicIngestion);
    private static readonly Counter<int> SuccessCounter = Meter.CreateCounter<int>("ingestion_success_count");
    private static readonly Counter<int> FailureCounter = Meter.CreateCounter<int>("ingestion_failure_count");
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>("ingestion_duration_seconds");


    public ComicCsvIngestor(IKafkaProducer producer)
    {
        _producer = producer;
    }

    public async Task IngestAsync(string csvPath)
    {
        var importId = Guid.NewGuid();

        var metricTags = TelemetryExtensions.BuildMetricTags(importId, "ComicCsvIngestorService", "UserUpload");




        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<ComicCsvRecord>().ToList();

        int successCount = 0;
        int failureCount = 0;
        var started = DateTimeOffset.UtcNow;

        foreach (var record in records)
        {
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

                var key = $"dead|{importId}";
                await _producer.ProduceAsync("comic-ingestion-dead-letter", key, deadLetter);
                failureCount++;

                FailureCounter.Add(1, metricTags);
                continue;
            }

            try
            {
                var comicEvent = record.ToFacet<ComicCsvRecord, ComicCsvRecordDto>();

                var publisherKey = record.PublisherName.Normalize("-");
                var seriesKey = record.SeriesName.Normalize("-");
                var key = $"{publisherKey}|{seriesKey}|{importId}";

                var envelope = new KafkaEnvelope<ComicCsvRecordDto>
                {
                    ImportId = importId.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Payload = comicEvent
                };

                await _producer.ProduceAsync("comic-imported", key, envelope);
                successCount++;

                SuccessCounter.Add(1, metricTags);
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

                var key = $"dead|{importId}";
                await _producer.ProduceAsync("comic-ingestion-dead-letter", key, deadLetter);
                failureCount++;

                FailureCounter.Add(1, metricTags);
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

        DurationHistogram.Record(durationSeconds, metricTags);


    }
}
