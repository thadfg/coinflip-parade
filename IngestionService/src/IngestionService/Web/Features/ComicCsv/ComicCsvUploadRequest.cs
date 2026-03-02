using Microsoft.AspNetCore.Http;

namespace IngestionService.Web.Features.ComicCsv;

public class ComicCsvUploadRequest
{
    public required IFormFile File { get; set; }
}
