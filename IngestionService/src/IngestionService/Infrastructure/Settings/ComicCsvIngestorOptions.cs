namespace IngestionService.Infrastructure.Settings;

public class ComicCsvIngestorOptions
{
    public const string SectionName = "ComicCsvIngestor";

    public string ImportedTopic { get; set; } = "comic-imported";
    public string DeadLetterTopic { get; set; } = "comic-ingestion-dead-letter";
    public string MetricsTopic { get; set; } = "comic-ingestion-metrics";
    public string ServiceName { get; set; } = "ComicCsvIngestorService";
    public string DefaultTrigger { get; set; } = "UserUpload";
    public string SourceSystem { get; set; } = "CsvImportService";
}
