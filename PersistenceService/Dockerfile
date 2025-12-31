# Base image: ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5067
EXPOSE 7086

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG NUGET_CONFIG=NuGet.local.config

WORKDIR /src

# Copy NuGet config (from PersistenceService folder)
COPY ${NUGET_CONFIG} /src/NuGet.config




# Copy the ENTIRE PersistenceService project tree
COPY PersistenceService/ /src/PersistenceService/

# For Debugging
#RUN ls -R /src

# Move into the project folder
WORKDIR /src/PersistenceService/src/PersistenceService


# Publish
RUN dotnet publish "PersistenceService.csproj" -c Release -o /out

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "PersistenceService.dll"]
