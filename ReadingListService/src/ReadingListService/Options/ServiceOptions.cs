namespace ReadingListService.Options;

public class ServiceOptions
{
    public const string SectionName = "Service";

    public string PathBase { get; set; } = "/readinglist";
    public string HealthCheckPath { get; set; } = "/api/reading-list/health";
}
