FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5283

# Install wget for healthcheck 
RUN apt-get update && apt-get install -y wget

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG NUGET_CONFIG=NuGet.local.config

WORKDIR /src

# Copy NuGet config
COPY ${NUGET_CONFIG} /src/NuGet.config

# Copy the ENTIRE project trees for both IngestionService and SharedLibrary
COPY IngestionService/ /src/IngestionService/

# Move into the project folder
WORKDIR /src/IngestionService/src/IngestionService

# Restore
RUN dotnet restore "IngestionService.csproj" --configfile /src/NuGet.config

# Publish
RUN dotnet publish "IngestionService.csproj" -c Release -o /out --no-restore


FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "IngestionService.dll"]
