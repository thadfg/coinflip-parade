using CsvHelper.Configuration.Attributes;

namespace IngestionService.Domain.Models;

public record ComicCsvRecord(
[Name("Publisher Name")] 
string PublisherName,
[Name("Series Name")] 
string SeriesName,
[Name("Full Title")] 
string FullTitle,
[Name("Release Date")] 
string? ReleaseDate,
[Name("In Collection")] 
string? InCollection
);


