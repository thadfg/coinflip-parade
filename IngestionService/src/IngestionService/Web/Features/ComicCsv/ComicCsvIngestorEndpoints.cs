using IngestionService.Application.Services;
using IngestionService.Web.Features.ComicCsv;
using Microsoft.AspNetCore.Mvc;

public static class ComicCsvIngestorEndpoints
{
    public static IEndpointRouteBuilder MapComicCsvIngestorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/comics/ingest-csv", async (
            [FromForm] ComicCsvUploadRequest request,
            ComicCsvIngestor ingestor,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("CsvUpload");

            logger.LogInformation("Received CSV upload request. File: {FileName}, Length: {Length}",
                request.File?.FileName, request.File?.Length);

            if (request.File == null || request.File.Length == 0)
            {
                logger.LogWarning("Upload failed: no file provided.");
                return Results.BadRequest("No CSV file uploaded.");
            }

            var tempFile = Path.GetTempFileName();
            logger.LogInformation("Temporary file created at {TempFile}", tempFile);

            try
            {
                using (var stream = File.Create(tempFile))
                {
                    await request.File.CopyToAsync(stream);
                }

                logger.LogInformation("File copied to temp location. Starting ingestion...");

                await ingestor.IngestAsync(tempFile);

                logger.LogInformation("Ingestion completed successfully.");
                return Results.Ok("CSV ingested successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting CSV");
                return Results.Problem($"Error ingesting CSV: {ex.Message}");
            }
            finally
            {
                File.Delete(tempFile);
                logger.LogInformation("Temporary file deleted.");
            }
        })
        .Accepts<ComicCsvUploadRequest>("multipart/form-data")
        .Produces(200)
        .Produces(400)
        .Produces(500)
        .DisableAntiforgery();

        return endpoints;
    }
}
