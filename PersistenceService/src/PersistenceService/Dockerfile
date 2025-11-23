# Use the official ASP.NET runtime as the base image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["PersistenceService/PersistenceService.csproj", "PersistenceService/"]
RUN dotnet restore "PersistenceService/PersistenceService.csproj"
COPY . .
WORKDIR "/src/PersistenceService"
RUN dotnet publish -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PersistenceService.dll"]
