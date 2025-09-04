using IngestionService.Domain.Models;
using Facet;


namespace IngestionService.Application.Facets;

[Facet(typeof(ComicCsvRecord))]
public partial class ComicCsvRecordDto
{
    public decimal? Value { get; set; }
    public string? CoverArtPath { get; set; }
}


