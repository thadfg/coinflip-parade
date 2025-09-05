using Facet;

[Facet(typeof(ComicCsvRecord))]
public partial class ComicCsvRecordDto
{
    public decimal? Value { get; set; }
    public string? CoverArtPath { get; set; }
}
