using IngestionService.Application.Services;
using IngestionService.Web.Features.ComicCsv;
using Microsoft.AspNetCore.Mvc;

namespace IngestionService.Web.Features.ComicCsv;
public static class ComicCsvIngestorEndpoints
{
    public static IEndpointRouteBuilder MapComicCsvIngestorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/comics/ingest-csv", async ([FromForm] ComicCsvUploadRequest request, ComicCsvIngestor ingestor) =>
        {
            if (request.File == null || request.File.Length == 0)
                return Results.BadRequest("No CSV file uploaded.");

            var tempFile = Path.GetTempFileName();

            using (var stream = File.Create(tempFile))
            {
                await request.File.CopyToAsync(stream);
            }

            try
            {
                await ingestor.IngestAsync(tempFile);
                return Results.Ok("CSV ingested successfully.");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error ingesting CSV: {ex.Message}");
            }
            finally
            {
                File.Delete(tempFile);
            }
        })
        .Accepts<ComicCsvUploadRequest>("multipart/form-data")
        .Produces(200)
        .Produces(400)
        .Produces(500)
        .DisableAntiforgery(); // ?? disables antiforgery for this endpoint;

        return endpoints;
    }
}
