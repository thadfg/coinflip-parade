using Microsoft.AspNetCore.Http;

namespace IngestionService.Web.Features.ComicCsv;

public class ComicCsvUploadRequest
{
    public IFormFile? File { get; set; }
}
