# Graph Report - coinflip-parade  (2026-10-10)

## Corpus Check
- 145 files · ~25,937 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 844 nodes · 1320 edges · 53 communities (47 shown, 6 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 66 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `36ec3107`
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
- ComicApp System Architecture
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
- ReadingListService.Data
- ReadingListController
- IComicRepository
- ReadingListService.Tests
- SearchOptions

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

## Communities (53 total, 6 thin omitted)

### Community 0 - "Task"
Cohesion: 0.17
Nodes (10): DateTime, Guid, List, Task, ComicRepository, DateTime, Guid, List (+2 more)

### Community 1 - "ValuationBackgroundWorker"
Cohesion: 0.05
Nodes (32): BackgroundService, ValuationService.Service, ValuationService.Controllers, ValuationService.Tests, ValuationService.Infrastructure, InlineData, ProcessStartInfo, Theory (+24 more)

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
Cohesion: 0.12
Nodes (15): ControllerBase, HttpGet, HttpPost, IActionResult, ValuationController, bool, ValuationControlService, Fact (+7 more)

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
Cohesion: 0.45
Nodes (4): Fact, IOptions, Task, ComicRepositoryTests

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
Nodes (9): DbSet, ModelBuilder, ReadingListDbContext, DateTime, Guid, ComicRecord, DateTime, Guid (+1 more)

### Community 21 - "PersistenceService.Tests"
Cohesion: 0.14
Nodes (14): NUnit (4.4.0), PersistenceService.Tests, coverlet.collector (6.0.2), Microsoft.EntityFrameworkCore.InMemory (9.0.9), Microsoft.NET.Test.Sdk (17.12.0), Moq (4.20.72), xunit (2.9.2), xunit.runner.visualstudio (2.8.2) (+6 more)

### Community 22 - "ReadingListService.Tests"
Cohesion: 0.14
Nodes (14): Microsoft.NET.Sdk.Web, ReadingListService, Microsoft.AspNetCore.OpenApi (9.0.0), Microsoft.EntityFrameworkCore (9.0.0), Microsoft.EntityFrameworkCore.Design (9.0.0), Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (9.0.0), Npgsql.EntityFrameworkCore.PostgreSQL (9.0.0), ReadingListService.Tests (+6 more)

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
Cohesion: 0.18
Nodes (10): IngestionService.Tests, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), Moq (4.20.72), OpenTelemetry.Instrumentation.Http (1.15.0), OpenTelemetry.Instrumentation.Runtime (1.15.0), xunit (2.4.2), xunit.runner.visualstudio (2.4.5) (+2 more)

### Community 29 - "SharedLibrary.Extensions"
Cohesion: 0.22
Nodes (5): SharedLibrary.Extensions, StringExtensions, Guid, TelemetryExtensions, TagList

### Community 31 - "ComicApp System Architecture"
Cohesion: 0.09
Nodes (22): 10. Architectural Benefits & System Characteristics, 1. Executive Summary, 2. High-Level Architecture Diagram, 3.1 IngestionService, 3.2 PersistenceService, 3.3 ReadingListService, 3.4 ValuationService, 3.5 SharedLibrary (+14 more)

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

### Community 48 - "ReadingListService.Data"
Cohesion: 0.19
Nodes (8): ReadingListService.Options, ReadingListService.Pages, ReadingListService.Models, ReadingListService.Data, ReadingListService.Controllers, ReadingListService.Dtos, string, ServiceOptions

### Community 49 - "ReadingListController"
Cohesion: 0.26
Nodes (9): ActionResult, Controller, Guid, HttpGet, HttpPost, IActionResult, List, Task (+1 more)

### Community 50 - "IComicRepository"
Cohesion: 0.22
Nodes (8): PageModel, IComicRepository, IActionResult, List, Task, IndexModel, Task, WeeklyModel

### Community 51 - "ReadingListService.Tests"
Cohesion: 0.40
Nodes (3): ReadingListService.Tests, Fact, UnitTest1

## Knowledge Gaps
- **148 isolated node(s):** `1. Executive Summary`, `2. High-Level Architecture Diagram`, `3.1 IngestionService`, `3.2 PersistenceService`, `3.3 ReadingListService` (+143 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `SharedLibrary.Models` connect `PersistenceService.Domain.Entities` to `.ToEntity`, `ValuationBackgroundWorker`, `KafkaProducer`?**
  _High betweenness centrality (0.166) - this node is a cross-community bridge._
- **Why does `ReadingListDbContext` connect `ReadingListDbContext` to `Task`, `ReadingListService.Data`, `ComicCollectionDbContext`, `ComicRepositoryTests`?**
  _High betweenness centrality (0.150) - this node is a cross-community bridge._
- **Why does `ComicRepository` connect `Task` to `ComicRepositoryTests`, `ReadingListService.Data`, `IComicRepository`, `ReadingListDbContext`, `SearchOptions`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **What connects `1. Executive Summary`, `2. High-Level Architecture Diagram`, `3.1 IngestionService` to the rest of the system?**
  _148 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ValuationBackgroundWorker` be split into smaller, more focused modules?**
  _Cohesion score 0.0546448087431694 - nodes in this community are weakly interconnected._
- **Should `PersistenceService.Domain.Entities` be split into smaller, more focused modules?**
  _Cohesion score 0.07247223845704266 - nodes in this community are weakly interconnected._
- **Should `KafkaProducer` be split into smaller, more focused modules?**
  _Cohesion score 0.044444444444444446 - nodes in this community are weakly interconnected._