using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace IngestionService.Domain.Models;

public record ComicCsvRecord(
    [Name("Publisher Name")] string PublisherName,
    [Name("Series Name")] string SeriesName,
    [Name("Full Title")] string FullTitle,
    [Name("Release Date")] string? ReleaseDate,
    [Name("In Collection")] string? InCollection
)
{
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


