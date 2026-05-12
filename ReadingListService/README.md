# ReadingListService

A microservice for managing comic reading lists, part of the Coinflip Parade project. This service provides a Web API and Razor Pages for tracking comic collections and reading progress.

## Overview

The `ReadingListService` is built with ASP.NET Core 9.0. It manages comic-related data using Entity Framework Core and PostgreSQL.

### Tech Stack

- **Language:** C# 13.0
- **Framework:** ASP.NET Core 9.0 (MVC, Razor Pages, Web API)
- **Data Access:** Entity Framework Core
- **Database:** PostgreSQL
- **Documentation:** OpenAPI (Swagger)

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) (or a running Docker instance)

## Setup

1.  **Clone the repository** (if not already done).
2.  **Configure the database connection**:
    Update the connection string in `src/ReadingListService/appsettings.Development.json` or set an environment variable.
    ```json
    "ConnectionStrings": {
      "Default": "Host=localhost;Database=comicdb;Username=your_user;Password=your_password"
    }
    ```
3.  **Restore dependencies**:
    ```bash
    dotnet restore
    ```
4.  **Apply database migrations**:
    ```bash
    dotnet ef database update --project src/ReadingListService
    ```

## Run Commands

### Start the Service

From the project root:
```bash
dotnet run --project src/ReadingListService
```
The service should be available at `http://localhost:5000` (or the configured port).

### OpenAPI (Swagger)

In development mode, you can access the OpenAPI documentation at:
- `http://localhost:<port>/openapi/v1.json`

## Scripts

- `dotnet build`: Compiles the project.
- `dotnet run`: Launches the service.
- `dotnet ef migrations add <MigrationName>`: Creates a new database migration.
- `dotnet ef database update`: Applies pending migrations to the database.

## Environment Variables

- `ConnectionStrings__Default`: Connection string for the PostgreSQL database.
- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`.

## Project Structure

```text
ReadingListService/
├── ReadingListService.sln    # Solution file
├── src/
│   └── ReadingListService/
│       ├── Controllers/      # API Controllers
│       ├── Data/             # DbContext, Migrations, Repositories
│       ├── Dtos/             # Data Transfer Objects
│       ├── Models/           # Entity Models
│       ├── Pages/            # Razor Pages
│       ├── Program.cs        # Entry point and configuration
│       └── appsettings.json  # Configuration files
└── ReadingListService.http   # HTTP request samples
```

## Tests

TODO: Add unit and integration tests. No tests were found for this service yet.

## License

TODO: Specify the license (e.g., MIT, Apache 2.0).
