using CsvHelper.Configuration.Attributes;
using SharedLibrary.Constants;
using System.Globalization;

namespace SharedLibrary.Models
{
    public record ComicCsvRecord
    {
        [Name("Publisher")]
        public string? PublisherName { get; init; }

        [Name("Series")]
        public string? SeriesName { get; init; }

        [Name("Full Title")]
        public string? FullTitle { get; init; }

        [Name("Release Date")]
        public string? ReleaseDate { get; init; }

        [Name("In Collection")]
        [Optional]
        public string? InCollection { get; init; }

        public bool IsValid(out string? error)
        {
            if (string.IsNullOrWhiteSpace(PublisherName))
            {
                error = "Publisher is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(SeriesName))
            {
                error = "Series is required.";
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
            if (!DateTime.TryParseExact(
                    ReleaseDate?.Trim(),
                    DateFormats.AcceptedFormats, // Pass the array here
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedDate))
            {
                error = $"ReleaseDate '{ReleaseDate}' is not in a recognized format ({DateFormats.DisplayFormats}).";
                return false;
            }
            error = null;
            return true;
        }
    }

}
