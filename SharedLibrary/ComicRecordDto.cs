namespace SharedLibrary.Models;

public class ComicRecordDto
{
    public string PublisherName { get; set; }
    public string SeriesName { get; set; }
    public string FullTitle { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? InCollection { get; set; }
    public decimal? Value { get; set; }
    public string? CoverArtPath { get; set; }
}

