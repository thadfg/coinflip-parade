namespace IngestionService.Domain.Models;

public record ComicCsvRecord(
string PublisherName,
string SeriesName,
string FullTitle,
string ReleaseDate,
string InCollection
);


