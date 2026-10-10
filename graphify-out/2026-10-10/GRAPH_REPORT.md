# Graph Report - coinflip-parade  (2026-08-23)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 830 nodes · 1311 edges · 49 communities (44 shown, 5 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 66 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b58fc1fa`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Task
- ValuationBackgroundWorker
- PersistenceService.Domain.Entities
- KafkaProducer
- ComicCollectionDbContext
- KafkaComicListener
- .UpsertBatchAsync
- .Start
- EventEntity
- IngestionService/Startup/DependenciesConfig.cs
- KafkaLogger
- .ToEntity
- ComicCsvIngestor
- ComicRepositoryTests
- ValuationService
- IngestionService
- http
- http
- http
- PersistenceService
- ReadingListDbContext
- PersistenceService.Tests
- ReadingListService.Tests
- KafkaLogHelper
- KafkaLoggerProvider
- KafkaLogger
- PersistenceService.Infrastructure.Logging
- http
- IngestionService.Tests
- SharedLibrary.Extensions
- ReadingListService
- .ConfigureCustomKestrel
- NullScope
- .InitializeAsync
- _ComicTable.cshtml
- _ComicTableRows.cshtml
- research_comicbookrealm.js
- research_ebay.js
- KafkaOptions
- Weekly.cshtml
- ReadingListService.Dtos.ComicSearchResultDto
- ReadingListService.Pages.IndexModel

## God Nodes (most connected - your core abstractions)
1. `KafkaComicListener` - 23 edges
2. `ValuationService` - 22 edges
3. `IngestionService` - 22 edges
4. `PersistenceService` - 18 edges
5. `PersistenceService.Domain.Entities` - 17 edges
6. `SharedLibrary.Models` - 17 edges
7. `ComicCsvIngestor` - 15 edges
8. `PersistenceService.Infrastructure` - 14 edges
9. `ValuationBackgroundWorker` - 13 edges
10. `KafkaLogger` - 13 edges

## Surprising Connections (you probably didn't know these)
- `ComicCsvIngestor` --references--> `IKafkaProducer`  [EXTRACTED]
  IngestionService/src/IngestionService/Application/Services/ComicCsvIngestor.cs → SharedLibrary/IKafkaProducer.cs
- `KafkaProducer` --implements--> `IKafkaProducer`  [EXTRACTED]
  IngestionService/src/IngestionService/Infrastructure/Kafka/KafkaProducer.cs → SharedLibrary/IKafkaProducer.cs
- `ValuationService` --references--> `Microsoft.NET.Sdk.Web`  [EXTRACTED]
  ValuationService/src/ValuationService/ValuationService.csproj → IngestionService/src/IngestionService/IngestionService.csproj
- `ValuationService` --references--> `net9.0`  [EXTRACTED]
  ValuationService/src/ValuationService/ValuationService.csproj → SharedLibrary/SharedLibrary.csproj
- `IngestionService` --references--> `net9.0`  [EXTRACTED]
  IngestionService/src/IngestionService/IngestionService.csproj → SharedLibrary/SharedLibrary.csproj

## Import Cycles
- None detected.

## Communities (49 total, 5 thin omitted)

### Community 0 - "Task"
Cohesion: 0.08
Nodes (31): ActionResult, Controller, ReadingListService.Pages, ReadingListService.Data, ReadingListService.Controllers, ReadingListService.Dtos, PageModel, Guid (+23 more)

### Community 1 - "ValuationBackgroundWorker"
Cohesion: 0.06
Nodes (29): BackgroundService, ValuationService.Infrastructure, InlineData, ProcessStartInfo, Theory, Task, IMcpClientWrapper, McpClientWrapper (+21 more)

### Community 2 - "PersistenceService.Domain.Entities"
Cohesion: 0.07
Nodes (29): PersistenceService.Startup, PersistenceService.Tests.TestDataGenerators, PersistenceService.Tests.TestContexts, PersistenceService.Config, PersistenceService.Application.Mappers, PersistenceService.Infrastructure.Configurations, SharedLibrary.Facet, PersistenceService.Tests.Integration (+21 more)

### Community 3 - "KafkaProducer"
Cohesion: 0.04
Nodes (29): PersistenceService.Infrastructure.Telemetry, IngestionService.Infrastructure.Kafka, SharedLibrary.Constants, IDisposable, IConfiguration, IServiceCollection, KafkaConfiguration, Counter (+21 more)

### Community 4 - "ComicCollectionDbContext"
Cohesion: 0.06
Nodes (26): DbContext, IDesignTimeDbContextFactory, DateTime, Guid, ProcessedEvent, DbSet, ModelBuilder, ComicCollectionDbContext (+18 more)

### Community 5 - "KafkaComicListener"
Cohesion: 0.07
Nodes (26): ConcurrentDictionary, ConsumeResult, IConsumer, Exception, Task, IKafkaLogHelper, CancellationToken, CancellationTokenSource (+18 more)

### Community 6 - ".UpsertBatchAsync"
Cohesion: 0.08
Nodes (29): IEntityTypeConfiguration, CancellationToken, Comic, EventId, Guid, IEnumerable, Task, IComicCollectionRepository (+21 more)

### Community 7 - ".Start"
Cohesion: 0.10
Nodes (18): ControllerBase, ValuationService.Service, ValuationService.Controllers, ValuationService.Tests, HttpGet, HttpPost, IActionResult, ValuationController (+10 more)

### Community 8 - "EventEntity"
Cohesion: 0.11
Nodes (19): Action, CancellationToken, IEnumerable, Task, IEventRepository, DateTimeOffset, Guid, EventEntity (+11 more)

### Community 9 - "IngestionService/Startup/DependenciesConfig.cs"
Cohesion: 0.07
Nodes (18): IngestionService.Startup, IngestionService.Infrastructure.Telemetry, IngestionService.Web.Features.ComicCsv, IngestionService.Application.Services, IEndpointRouteBuilder, IFormFile, DateTime, Meter (+10 more)

### Community 10 - "KafkaLogger"
Cohesion: 0.10
Nodes (17): IngestionService.Infrastructure.Logging, IExternalScopeProvider, ILoggerProvider, EventId, Exception, Func, IDisposable, ILogger (+9 more)

### Community 11 - ".ToEntity"
Cohesion: 0.11
Nodes (14): ComicRecordMapper, Guid, EventEntityMapper, ComicRecordMapperTests, Fact, Fact, Task, IntegrationTestEndToEndFlow (+6 more)

### Community 12 - "ComicCsvIngestor"
Cohesion: 0.13
Nodes (16): double, Histogram, ActivitySource, Counter, DateTimeOffset, Guid, Meter, ObservableGauge (+8 more)

### Community 13 - "ComicRepositoryTests"
Cohesion: 0.16
Nodes (12): ReadingListService.Options, ReadingListService.Tests, string, SearchOptions, string, ServiceOptions, Fact, IOptions (+4 more)

### Community 14 - "ValuationService"
Cohesion: 0.10
Nodes (20): Microsoft.EntityFrameworkCore.Relational (9.0.0), ModelContextProtocol (1.2.0), ValuationService, Microsoft.AspNetCore.OpenApi (9.0.0), Microsoft.EntityFrameworkCore (9.0.0), Microsoft.EntityFrameworkCore.InMemory (9.0.0), Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (9.0.0), Microsoft.NET.Test.Sdk (18.3.0) (+12 more)

### Community 15 - "IngestionService"
Cohesion: 0.11
Nodes (19): IngestionService, Confluent.Kafka (2.11.1), CsvHelper (33.1.0), Facet (2.5.0), Facet.Extensions.EFCore (2.5.0), Microsoft.AspNetCore.OpenApi (9.0.8), OpenTelemetry.Exporter.OpenTelemetryProtocol (1.10.0), OpenTelemetry.Exporter.Prometheus.AspNetCore (1.10.0-beta.1) (+11 more)

### Community 16 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 17 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 18 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 19 - "PersistenceService"
Cohesion: 0.13
Nodes (15): EFCore.BulkExtensions.PostgreSql (9.0.*), PersistenceService, Confluent.Kafka (2.11.1), Microsoft.AspNetCore.OpenApi (9.0.6), Microsoft.EntityFrameworkCore (9.0.*), Microsoft.EntityFrameworkCore.Design (9.0.*), Npgsql.EntityFrameworkCore.PostgreSQL (9.0.*), OpenTelemetry.Exporter.OpenTelemetryProtocol (1.15.0) (+7 more)

### Community 20 - "ReadingListDbContext"
Cohesion: 0.18
Nodes (10): ReadingListService.Models, DbSet, ModelBuilder, ReadingListDbContext, DateTime, Guid, ComicRecord, DateTime (+2 more)

### Community 21 - "PersistenceService.Tests"
Cohesion: 0.25
Nodes (8): NUnit (4.4.0), PersistenceService.Tests, coverlet.collector (6.0.2), Microsoft.EntityFrameworkCore.InMemory (9.0.9), Microsoft.NET.Test.Sdk (17.12.0), Moq (4.20.72), xunit (2.9.2), xunit.runner.visualstudio (2.8.2)

### Community 22 - "ReadingListService.Tests"
Cohesion: 0.17
Nodes (13): ReadingListService.Tests, coverlet.collector (6.0.2), Microsoft.EntityFrameworkCore.InMemory (9.0.0), Microsoft.NET.Test.Sdk (17.12.0), Moq (4.20.72), xunit (2.9.2), xunit.runner.visualstudio (2.8.2), net9.0 (+5 more)

### Community 23 - "KafkaLogHelper"
Cohesion: 0.18
Nodes (9): Exception, IConfiguration, ILogger, IProducer, Task, KafkaLogHelper, Exception, Task (+1 more)

### Community 24 - "KafkaLoggerProvider"
Cohesion: 0.20
Nodes (8): Channel, bool, CancellationToken, CancellationTokenSource, ILogger, IProducer, Task, KafkaLoggerProvider

### Community 25 - "KafkaLogger"
Cohesion: 0.25
Nodes (8): DateTime, EventId, Exception, Func, LogLevel, string, KafkaLogger, KafkaLogMessage

### Community 26 - "PersistenceService.Infrastructure.Logging"
Cohesion: 0.20
Nodes (6): PersistenceService.Infrastructure.Logging, Exception, IProducer, string, Task, KafkaLogSink

### Community 27 - "http"
Cohesion: 0.20
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, profiles, http (+1 more)

### Community 28 - "IngestionService.Tests"
Cohesion: 0.20
Nodes (10): IngestionService.Tests, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), Moq (4.20.72), OpenTelemetry.Instrumentation.Http (1.15.0), OpenTelemetry.Instrumentation.Runtime (1.15.0), xunit (2.4.2), xunit.runner.visualstudio (2.4.5) (+2 more)

### Community 29 - "SharedLibrary.Extensions"
Cohesion: 0.22
Nodes (5): SharedLibrary.Extensions, StringExtensions, Guid, TelemetryExtensions, TagList

### Community 31 - "ReadingListService"
Cohesion: 0.29
Nodes (7): Microsoft.NET.Sdk.Web, ReadingListService, Microsoft.AspNetCore.OpenApi (9.0.0), Microsoft.EntityFrameworkCore (9.0.0), Microsoft.EntityFrameworkCore.Design (9.0.0), Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (9.0.0), Npgsql.EntityFrameworkCore.PostgreSQL (9.0.0)

### Community 32 - ".ConfigureCustomKestrel"
Cohesion: 0.40
Nodes (3): HostingConfig, KestrelServerOptions, WebHostBuilderContext

### Community 33 - "NullScope"
Cohesion: 0.40
Nodes (3): NullScope, IDisposable, NullScope

### Community 34 - ".InitializeAsync"
Cohesion: 0.40
Nodes (3): IServiceProvider, Task, DbInitializer

### Community 35 - "_ComicTable.cshtml"
Cohesion: 0.50
Nodes (3): ComicSearchResultDto, List<ComicSearchResultDto>, ReadingListService.Dtos

### Community 36 - "_ComicTableRows.cshtml"
Cohesion: 0.50
Nodes (3): ComicSearchResultDto, List<ComicSearchResultDto>, ReadingListService.Dtos

### Community 38 - "research_ebay.js"
Cohesion: 0.67
Nodes (3): { chromium }, getEbayAveragePrice(), title

## Knowledge Gaps
- **131 isolated node(s):** `KafkaSettings`, `Program`, `ReadingListService.Controllers`, `Microsoft.EntityFrameworkCore.Relational (9.0.0)`, `ModelContextProtocol (1.2.0)` (+126 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `SharedLibrary.Models` connect `PersistenceService.Domain.Entities` to `.ToEntity`, `KafkaProducer`?**
  _High betweenness centrality (0.181) - this node is a cross-community bridge._
- **Why does `ReadingListDbContext` connect `ReadingListDbContext` to `Task`, `ComicCollectionDbContext`, `ComicRepositoryTests`?**
  _High betweenness centrality (0.157) - this node is a cross-community bridge._
- **Why does `ComicRepository` connect `Task` to `ReadingListDbContext`, `ComicRepositoryTests`?**
  _High betweenness centrality (0.090) - this node is a cross-community bridge._
- **What connects `KafkaSettings`, `Program`, `ReadingListService.Controllers` to the rest of the system?**
  _131 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Task` be split into smaller, more focused modules?**
  _Cohesion score 0.07547169811320754 - nodes in this community are weakly interconnected._
- **Should `ValuationBackgroundWorker` be split into smaller, more focused modules?**
  _Cohesion score 0.06009783368273934 - nodes in this community are weakly interconnected._
- **Should `PersistenceService.Domain.Entities` be split into smaller, more focused modules?**
  _Cohesion score 0.0711864406779661 - nodes in this community are weakly interconnected._