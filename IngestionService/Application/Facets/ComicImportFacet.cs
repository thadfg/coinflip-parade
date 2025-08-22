using IngestionService.Domain.Models;
using Facet;
using System.Globalization;


namespace IngestionService.Application.Facets;

[Facet(typeof(ComicCsvRecord))]
public partial class ComicCsvRecordDto
{
    /*public DateTime ReleaseDate { get; set; } // <-- Ensure this is DateTime
    public ComicCsvRecordDto(ComicCsvRecord source)
    {
        PublisherName = source.PublisherName;
        SeriesName = source.SeriesName;
        FullTitle = source.FullTitle;
        InCollection = source.InCollection;

        // Parse ReleaseDate safely
        ReleaseDate = DateTime.TryParse(source.ReleaseDate, out var parsedDate)
            ? parsedDate
            : DateTime.MinValue; // Or throw/log if you prefer strict validation
    }*/
}

