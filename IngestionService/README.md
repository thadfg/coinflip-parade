# IngestionService

IngestionService is a .NET 9.0 microservice responsible for ingesting comic data from CSV files and publishing them as events to a Kafka topic. It is part of the Coinflip Parade project.

## Features

- **CSV Ingestion**: High-performance CSV parsing using `CsvHelper`.
- **Kafka Integration**: Publishes ingested records to Kafka with support for distributed tracing and correlation IDs.
- **Observability**:
    - **OpenTelemetry**: Integrated tracing, metrics, and logging.
    - **Health Checks**: Standard ASP.NET Core health checks (`/health`, `/live`, `/ready`).
    - **Metrics**: Custom metrics for ingestion status and Kafka production.
- **Resilience**: Integrated with Polly for handling transient failures.
- **API Documentation**: OpenAPI (Swagger) and Scalar integration for API exploration.

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Kafka Cluster (configured via `appsettings.json`)
- OpenTelemetry Collector (optional, for telemetry export)

### Configuration

The service is configured via `appsettings.json`. Key settings include:

- `Kafka`: Connection strings and topic configurations.
- `OTEL_SETTINGS`: OpenTelemetry collector endpoints and service information.

### Running the Service

```bash
dotnet run --project src/IngestionService/IngestionService.csproj
```

### API Endpoints

- `POST /api/comics/ingest-csv`: Upload a CSV file for ingestion.
- `GET /api/comics/ping`: Simple connectivity check and uptime metrics.
- `GET /health`: General health check.

## Project Structure

- `src/IngestionService/Application`: Contains business logic, including the `ComicCsvIngestor`.
- `src/IngestionService/Infrastructure`: External integrations (Kafka, Telemetry, Logging).
- `src/IngestionService/Web`: API endpoints and middleware configuration.
- `tests/IngestionService.Tests`: Unit and integration tests.

## Development

### Running Tests

```bash
dotnet test
```
