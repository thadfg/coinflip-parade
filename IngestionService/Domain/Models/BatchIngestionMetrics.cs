namespace IngestionService.Domain.Models;

public class BatchIngestionMetrics
{
    public string ImportId { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }

    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }

    public TimeSpan Duration => CompletedAt - StartedAt;

    // Optional: Add tags or source info for observability
    public string SourceSystem { get; set; }  // e.g., "CsvImportService"
    public string TriggeredBy { get; set; }   // e.g., "ScheduledJob" or "UserUpload"
}

