# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory to root of source tree
WORKDIR /src

# Copy entire solution context
COPY . .

# Optional: Inspect structure during build
RUN find /src

# Navigate to actual project folder
WORKDIR /src/IngestionService/src/IngestionService

# Restore, build, and publish
RUN dotnet restore "IngestionService.csproj"
RUN dotnet build "IngestionService.csproj" -c Release -o /app/build
RUN dotnet publish "IngestionService.csproj" -c Release -o /app/publish

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IngestionService.dll"]
