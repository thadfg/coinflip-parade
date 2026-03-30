# IngestionService Architecture

This document provides a high-level overview of the architectural components and data flow within the IngestionService.

## System Architecture

The IngestionService is designed as a focused microservice following clean architecture principles, emphasizing separation of concerns and observability.

### Key Components

- **Web Layer**: Defines minimal API endpoints (`ComicCsvIngestorEndpoints`) to receive HTTP requests and manage file uploads.
- **Application Layer**: Contains the core business logic (`ComicCsvIngestor`). It coordinates the ingestion process, parsing CSV data, generating stable IDs, and interfacing with infrastructure services.
- **Infrastructure Layer**:
  - **Kafka Integration**: Implements reliable messaging using `Confluent.Kafka`. Handles serialization, header injection for tracing, and correlation ID propagation.
  - **Telemetry**: Configures OpenTelemetry to provide full-stack observability, including traces, custom metrics, and log aggregation via OTLP.
- **Shared Library**: Leverages a common project (`SharedLibrary`) for reusable models, Kafka settings, and constants.

## Data Flow: CSV Ingestion

1. **Request Reception**: An HTTP POST request is received at `/api/comics/ingest-csv` with a multipart form-data CSV file.
2. **Temporary Storage**: The file is briefly stored on the local filesystem as a temporary file.
3. **Ingestion Process**:
   - `ComicCsvIngestor` parses the CSV using `CsvHelper`.
   - Each record is mapped to a domain model.
   - Stable `ComicId`s are generated based on record properties (Publisher, Series, Title, Date).
4. **Kafka Production**: Each valid record is serialized and published to the `comic-ingestion` Kafka topic.
5. **Observability**:
   - A distributed trace is started for the ingestion request.
   - Spans are created for individual record processing and Kafka production.
   - Metrics such as `kafka_messages_produced_total` and `service_uptime_seconds` are tracked and exported.
6. **Cleanup**: Temporary files are deleted, and a summary response is returned to the client.

## Resilience and Error Handling

- **Polly Policies**: (If implemented) Used to handle transient errors during Kafka communication.
- **Dead Letter Logic**: Failed ingestion attempts or invalid records are handled by `ProduceDeadLetterAsync`, which publishes error details to a specific Kafka topic for further investigation.
- **Structured Logging**: All significant events and errors are logged with structured data and correlation IDs to facilitate troubleshooting.
