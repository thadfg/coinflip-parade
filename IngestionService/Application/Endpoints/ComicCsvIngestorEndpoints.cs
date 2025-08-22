using IngestionService.Application.Services;

public static class ComicCsvIngestorEndpoints
{
    public static IEndpointRouteBuilder MapComicCsvIngestorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/comics/ingest-csv", async (IFormFile file, ComicCsvIngestor ingestor) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            var tempFile = Path.GetTempFileName();

            using (var stream = File.Create(tempFile))
            {
                await file.CopyToAsync(stream);
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
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        return endpoints;
    }
}