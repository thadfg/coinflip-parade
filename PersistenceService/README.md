# PersistenceService

The `PersistenceService` is a robust C# .NET 9.0 microservice responsible for consuming comic-related events from Kafka and persisting them into a PostgreSQL database. It is a critical component of the `coinflip-parade` ecosystem, ensuring data durability and providing a historical record of ingested comic data.

## Features

- **Kafka Consumption**: High-performance, asynchronous consumption of comic event streams.
- **Data Persistence**: Uses Entity Framework Core with PostgreSQL for reliable data storage.
- **Resiliency**: Implements Polly retry policies for Kafka connections and database operations.
- **Observability**: Fully instrumented with OpenTelemetry for metrics (Prometheus) and distributed tracing (OTLP).
- **Health & Readiness**: Built-in health checks and readiness probes to ensure the service and its dependencies (DB, Kafka) are operational.
- **Batch Processing**: Optimized database writes using buffered batching for improved throughput.

## Project Structure

- `src/PersistenceService/Application`: Contains interfaces and mappers for data transformation.
- `src/PersistenceService/Domain`: Defines the core entities and domain models.
- `src/PersistenceService/Infrastructure`: Implements the technical details like Kafka listeners, database contexts, repositories, and observability.
- `src/PersistenceService/Startup`: Handles dependency injection and service configuration.
- `tests/PersistenceService.Tests`: Comprehensive test suite including unit and integration tests.

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/)
- [Apache Kafka](https://kafka.apache.org/)

### Configuration

Configuration is managed via `appsettings.json` or environment variables. Key settings include:

- `ConnectionStrings:EventDb`: PostgreSQL connection string.
- `Kafka:BootstrapServers`: Kafka broker address.
- `KafkaListener`: Tuning parameters for batch size, flush intervals, and retry delays.

### Running Locally

```bash
cd src/PersistenceService
dotnet run
```

## Documentation

For a deeper dive into the architecture and design decisions, please refer to:
- [Architecture Documentation](Architecture.md)
