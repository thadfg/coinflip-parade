FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/IngestionService/src

# 👇 Copy SharedLibrary to match the expected relative path
COPY ../../../../SharedLibrary ../../../SharedLibrary

# 👇 Copy IngestionService source
COPY . .

# 🔍 Diagnostic: Inspect folder structure
RUN find /src

# 👇 Restore and build using the existing project reference
RUN dotnet restore "IngestionService.csproj"
RUN dotnet build "IngestionService.csproj" -c Release -o /app/build
RUN dotnet publish "IngestionService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IngestionService.dll"]
