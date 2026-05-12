namespace ReadingListService.Options;

public class SearchOptions
{
    public const string SectionName = "Search";

    public int DefaultPageSize { get; set; } = 50;
    public string ExclusionPattern { get; set; } = "%Annual%";
}
