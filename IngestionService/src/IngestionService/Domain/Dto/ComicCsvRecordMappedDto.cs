namespace IngestionService.Application.Models
{
    public class ComicCsvRecordMappedDto
    {
        public string PublisherName { get; set; }
        public string SeriesName { get; set; }
        public string FullTitle { get; set; }
        public bool? InCollection { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}
