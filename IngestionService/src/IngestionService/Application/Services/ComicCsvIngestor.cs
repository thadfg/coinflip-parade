using SharedLibrary.Constants;
using CsvHelper;
using Facet.Extensions;
using IngestionService.Domain.Models;
using SharedLibrary.Extensions;
using SharedLibrary.Facet;
using SharedLibrary.Kafka;
using SharedLibrary.Models;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using OpenTelemetry.Trace;

namespace IngestionService.Application.Services;

public class ComicCsvIngestor
{
    private readonly IKafkaProducer _producer;

    // --- Metrics Setup ---
    private static readonly Meter Meter = new(MeterNames.ComicIngestion);
    private static readonly Counter<long> IngestionSuccess = Meter.CreateCounter<long>("ingestion_success");
    private static readonly Counter<long> IngestionFailure = Meter.CreateCounter<long>("ingestion_failure");
    private static readonly Histogram<double> IngestionDuration = Meter.CreateHistogram<double>("ingestion_duration_seconds", "s");
    
    private static readonly DateTimeOffset ServiceStart = DateTimeOffset.UtcNow;
    
    public static DateTimeOffset ServiceStartTime => ServiceStart;
    private static double _lastSuccessTimestamp = 0;
    private static readonly ObservableGauge<double> LastSuccessTimestamp = Meter.CreateObservableGauge("last_success_timestamp", () => _lastSuccessTimestamp);

    // --- Tracing Setup ---
    private static readonly ActivitySource ActivitySource = new("IngestionService.ComicCsvIngestor");

    public ComicCsvIngestor(IKafkaProducer producer)
    {
        _producer = producer;
    }

    public async Task IngestAsync(string csvPath)
    {
        // 1. Start Root Activity: Wraps the entire file processing logic
        using var activity = ActivitySource.StartActivity("Ingest.Batch.Process", ActivityKind.Internal);
        
        
        
        var importId = Guid.NewGuid();
        var importIdStr = importId.ToString();
        var service = "ComicCsvIngestorService";
        var trigger = "UserUpload";
        
        activity?.SetTag("import.id", importIdStr);
        activity?.SetTag("file.path", csvPath);

        var started = DateTimeOffset.UtcNow;
        int successCount = 0;
        int failureCount = 0;

        try
        {
            using var reader = new StreamReader(csvPath);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<ComicCsvRecord>().ToList();
            
            activity?.SetTag("record.count", records.Count);

            foreach (var record in records)
            {
                var correlationId = Guid.NewGuid().ToString();
                
                // Validation Phase
                if (!record.IsValid(out var validationError))
                {
                    await ProduceDeadLetterAsync(record, importIdStr, correlationId, $"Validation failed: {validationError}", "Validation");
                    failureCount++;
                    RecordMetric(IngestionFailure, importIdStr, service, trigger);
                    continue;
                }

                var comicId = GenerateComicId(record.PublisherName, record.SeriesName, record.FullTitle, record.ReleaseDate!).ToString();
                var comicIdStr = comicId;

                // Processing Phase
                try
                {
                    // Normalize date to YYYY-MM-DD
                    string normalizedDate = record.ReleaseDate!;
                    if (DateTime.TryParseExact(record.ReleaseDate?.Trim(), DateFormats.AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        normalizedDate = parsedDate.ToString("yyyy-MM-dd");
                    }

                    var comicEvent = record with { ReleaseDate = normalizedDate };
                    var comicDto = comicEvent.ToFacet<ComicCsvRecord, ComicCsvRecordDto>();
                    
                    var publisherKey = record.PublisherName.Normalize("-");
                    var seriesKey = record.SeriesName.Normalize("-");
                    var key = comicIdStr;

                    var envelope = new KafkaEnvelope<ComicCsvRecordDto>
                    {
                        
                        ImportId = importIdStr,
                        Timestamp = DateTime.UtcNow,
                        Payload = comicDto
                    };

                    // 2. Child Span: Specific to the Kafka Produce operation
                    using (var childActivity = ActivitySource.StartActivity("Ingest.Record.Produce", ActivityKind.Producer))
                    {
                        childActivity?.SetTag("messaging.destination", "comic-imported");
                        childActivity?.SetTag("import.id", importIdStr);
                        childActivity?.SetTag("publisher", publisherKey);
                        childActivity?.AddBaggage("correlation.id", correlationId);

                        await _producer.ProduceAsync("comic-imported", key, envelope, correlationId);
                    }

                    successCount++;
                    RecordMetric(IngestionSuccess, importIdStr, service, trigger);
                }
                catch (Exception ex)
                {
                    // 3. Log Error to the Root Activity
                    activity?.RecordException(ex);
                    
                    await ProduceDeadLetterAsync(record, importIdStr, correlationId, ex.Message, "RuntimeError");
                    failureCount++;
                    RecordMetric(IngestionFailure, importIdStr, service, trigger);
                }
            }

            await FinalizeIngestion(importIdStr, started, successCount, failureCount);
        }
        catch (Exception ex)
        {
            // If the CSV reading itself fails
            activity?.SetStatus(ActivityStatusCode.Error, "Batch processing failed");
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            var durationSeconds = (DateTimeOffset.UtcNow - started).TotalSeconds;
            IngestionDuration.Record(durationSeconds, new TagList { { "import_id", importIdStr }, { "service", service } });
        }
    }
    
    // Example Logic for Ingestion Service
    public Guid GenerateComicId(string publisher, string series, string title, string date)
    {
        if (string.IsNullOrWhiteSpace(publisher))
            throw new ArgumentException("Publisher is required.", nameof(publisher));

        if (string.IsNullOrWhiteSpace(series))
            throw new ArgumentException("Series is required.", nameof(series));

        /*if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));*/

        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Release date is required.", nameof(date));

        if (!DateTime.TryParseExact(date, DateFormats.AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            throw new FormatException($"Invalid release date format: '{date}'");

        var normalizedInput =
            $"{publisher.Trim().ToLowerInvariant()}-" +
            $"{series.Trim().ToLowerInvariant()}-" +
            $"{title.Trim().ToLowerInvariant()}-" +
            $"{parsedDate:yyyyMMdd}";

        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(normalizedInput));
        return new Guid(hash);
    }

    private async Task ProduceDeadLetterAsync(ComicCsvRecord record, string importId, string correlationId, string reason, string errorType)
    {
        // 4. Child Span: Specific to Dead Lettering
        using var dlqActivity = ActivitySource.StartActivity("Ingest.Record.DeadLetter", ActivityKind.Producer);
        dlqActivity?.SetTag("error.type", errorType);
        dlqActivity?.SetTag("error.reason", reason);
        dlqActivity?.AddBaggage("correlation.id", correlationId);

        var deadLetter = new DeadLetterEnvelope<ComicCsvRecord>
        {
            ImportId = importId,
            Timestamp = DateTime.UtcNow,
            Reason = reason,
            FailedPayload = record,
            EventType = "Ingestion"
        };

        var key = $"dead|{importId}|{correlationId}";
        await _producer.ProduceAsync("comic-ingestion-dead-letter", key, deadLetter, correlationId);
    }

    private async Task FinalizeIngestion(string importId, DateTimeOffset started, int success, int failure)
    {
        var metrics = new BatchIngestionMetrics
        {
            ImportId = importId,
            StartedAt = started,
            CompletedAt = DateTimeOffset.UtcNow,
            TotalRecords = success + failure,
            SuccessfulRecords = success,
            FailedRecords = failure,
            SourceSystem = "CsvImportService",
            TriggeredBy = "UserUpload"
        };

        await _producer.ProduceAsync("comic-ingestion-metrics", importId, metrics);
        _lastSuccessTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private void RecordMetric(Counter<long> counter, string importId, string service, string trigger)
    {
        var tags = new TagList { { "import_id", importId }, { "service", service }, { "trigger", trigger } };
        counter.Add(1, tags);
    }
}