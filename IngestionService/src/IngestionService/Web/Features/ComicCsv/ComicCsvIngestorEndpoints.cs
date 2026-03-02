using System;
using System.IO;
using IngestionService.Application.Services;
using IngestionService.Web.Features.ComicCsv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class ComicCsvIngestorEndpoints
{
    public static IEndpointRouteBuilder MapComicCsvIngestorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/comics/ingest-csv", async (
            [FromForm] ComicCsvUploadRequest? request,
            ComicCsvIngestor ingestor,
            ILogger<ComicCsvIngestor>? logger) =>
        {
            //var logger = loggerFactory.CreateLogger("CsvUpload");
            
            if (logger == null) 
            {
                // We can't even log here, so we have to return a plain 500
                return Results.Problem("Dependency Injection Error: Logger is null.");
            }

            logger.LogInformation("Received CSV upload request. File: {FileName}, Length: {Length}",
                request?.File?.FileName ?? "N/A", 
                request?.File?.Length ?? 0);
            
            if (request == null)
            {
                logger.LogError("CRITICAL: Model Binding failed. The 'request' object is null.");
                return Results.BadRequest("The server could not parse the form data. Check your 'File' key.");
            }

            if (request.File == null || request.File.Length == 0)
            {
                logger.LogWarning("No file found in the 'File' field.");
                return Results.BadRequest("Missing file.");
            }
            
            logger.LogInformation("Processing File: {FileName}, Length: {Length}", 
                request.File.FileName, request.File.Length);
            
            if ( request.File.Length == 0)
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
                logger.LogError(ex, "Ingestion failed for file {File}", request.File.FileName);
                return Results.Problem($"Error ingesting CSV: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                logger.LogInformation("Temporary file deleted.");
            }
        })
        .Accepts<ComicCsvUploadRequest>("multipart/form-data")
        .Produces(200)
        .Produces(400)
        .Produces(500)
        .DisableAntiforgery();
        
        // Ping/Health endpoint here
        endpoints.MapGet("/api/comics/ping", () => 
        {
            // Touching the ingestor here forces the static metrics to register
            // if they haven't already.
            return Results.Ok(new { 
                Status = "Online",
                UptimeSeconds = (DateTimeOffset.UtcNow - ComicCsvIngestor.ServiceStartTime).TotalSeconds,
                Time = DateTimeOffset.UtcNow 
            });
        });

        return endpoints;
    }
}
