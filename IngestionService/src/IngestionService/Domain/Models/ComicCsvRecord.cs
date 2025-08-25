using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace IngestionService.Domain.Models;

public record ComicCsvRecord
{
    [Name("Publisher Name")]
    public string PublisherName { get; init; }

    [Name("Series Name")]
    public string SeriesName { get; init; }

    [Name("Full Title")]
    public string FullTitle { get; init; }

    [Name("Release Date")]
    public string? ReleaseDate { get; init; }

    [Name("In Collection")]
    public string? InCollection { get; init; }

    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(PublisherName))
        {
            error = "PublisherName is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(SeriesName))
        {
            error = "SeriesName is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(FullTitle))
        {
            error = "FullTitle is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(ReleaseDate))
        {
            error = "ReleaseDate is required.";
            return false;
        }
        if (!DateTime.TryParseExact(ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            error = "ReleaseDate must be in YYYY-MM-DD format.";
            return false;
        }
        error = null;
        return true;
    }
}
