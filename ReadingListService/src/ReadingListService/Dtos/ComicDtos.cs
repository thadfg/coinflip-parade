namespace ReadingListService.Dtos;

public class ComicSearchResultDto
{
    public Guid Id { get; set; }
    public string FullTitle { get; set; } = string.Empty;
    public string? SeriesName { get; set; }
    public string? PublisherName { get; set; }
    public string? IssueNumber { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public bool IsRead { get; set; }
}

public class WeeklyReadingListViewDto
{
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public List<ComicSearchResultDto> Comics { get; set; } = new();
}
