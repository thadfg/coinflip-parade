using CsvHelper.Configuration.Attributes;
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
            string[] acceptedFormats = { "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy", "dd-MMM-yy" };

            if (!DateTime.TryParseExact(
                    ReleaseDate?.Trim(),
                    acceptedFormats, // Pass the array here
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedDate))
            {
                error = $"ReleaseDate '{ReleaseDate}' is not in a recognized format (YYYY-MM-DD or MM/DD/YYYY or M/D/YYYY).";
                return false;
            }
            error = null;
            return true;
        }
    }

}
