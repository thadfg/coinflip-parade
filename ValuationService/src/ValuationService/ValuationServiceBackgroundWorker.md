# ValuationService

ValuationService is a background-processing ASP.NET Core service that periodically evaluates comic book records and updates their estimated value in a PostgreSQL database.

## Overview

The service:

- runs as an ASP.NET Core hosted application
- uses Entity Framework Core with PostgreSQL
- processes comic records in small batches
- sends research prompts to a Playwright MCP-based client
- parses returned results and stores the latest valuation

## Technology Stack

- .NET 9
- ASP.NET Core
- Entity Framework Core 9
- PostgreSQL
- Playwright MCP integration
- C# 13

## Project Structure

- `Program.cs` - application startup and dependency registration
- `Service/ValuationBackgroundWorker.cs` - background worker that performs valuation runs
- `Infrastructure/ComicDbContext.cs` - EF Core database context
- `Infrastructure/McpClientWrapper.cs` - wrapper for MCP-based research execution
- `appsettings.json` - application configuration

## How It Works

1. The hosted background worker starts when the application runs.
2. It checks the database for comic records that:
   - have no value yet, or
   - were last updated more than 30 days ago
3. It processes up to 10 records per cycle.
4. For each record, it generates a research prompt.
5. The MCP client executes the research workflow.
6. The returned text is parsed for a numeric value.
7. If a value is found, the record is updated in the database.

## Database

The service uses a `ComicDbContext` connected to a PostgreSQL database.

Default schema:

- `comics`

The application expects a `ComicRecordEntity` model available from the shared domain library.

## Configuration

The database connection is currently configured in `Program.cs` with a PostgreSQL connection string.

Typical settings to review:

- host
- port
- database name
- username
- password

You should move secrets into configuration or environment variables before production use.

## Running the Service

### Prerequisites

- .NET 9 SDK
- PostgreSQL running locally or remotely
- required database schema and shared models available
- Node.js / Playwright MCP dependencies available for research execution

### Start
