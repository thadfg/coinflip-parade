# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Accept optional NuGet config file
ARG NUGET_CONFIG=IngestionService/NuGet.local.config
COPY ${NUGET_CONFIG} ./NuGet.config


# Set working directory to root of source tree
WORKDIR /src

# Copy entire solution context
COPY . .

# Copy NuGet config if present
COPY IngestionService/NuGet.local.config ./NuGet.config

# Optional: Inspect structure during build
RUN find /src

# Navigate to actual project folder
WORKDIR /src/IngestionService/src/IngestionService

# Restore using injected config
RUN dotnet restore "IngestionService.csproj" --configfile /src/NuGet.config

# Build and publish
RUN dotnet build "IngestionService.csproj" -c Release -o /app/build
RUN dotnet publish "IngestionService.csproj" -c Release -o /app/publish

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IngestionService.dll"]
