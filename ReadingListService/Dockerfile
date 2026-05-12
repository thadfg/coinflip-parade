# Base image: ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG NUGET_CONFIG=NuGet.local.config

WORKDIR /src

# Copy NuGet config (from root)
COPY ${NUGET_CONFIG} /src/NuGet.config

# Copy the ENTIRE ReadingListService project tree
COPY ReadingListService/ /src/ReadingListService/

# Move into the project folder
WORKDIR /src/ReadingListService/src/ReadingListService

# Publish
RUN dotnet publish "ReadingListService.csproj" -c Release -o /out

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "ReadingListService.dll"]
