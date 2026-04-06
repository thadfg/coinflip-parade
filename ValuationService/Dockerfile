FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5283

# Install wget for health checks and necessary libraries for Playwright
RUN apt-get update && apt-get install -y \
    wget \
    libgbm1 \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpangocairo-1.0-0 \
    libpango-1.0-0 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install nodejs in build stage to use npm
RUN apt-get update && apt-get install -y curl \
    && curl -sL https://deb.nodesource.com/setup_18.x | bash - \
    && apt-get install -y nodejs

# Copy NuGet config
COPY NuGet.local.config /src/NuGet.config

# Copy the ENTIRE ValuationService project tree
COPY ValuationService/ /src/ValuationService/

# Copy the research script to the final app directory
COPY ValuationService/src/ValuationService/research_comicbookrealm.js /app/research_comicbookrealm.js

# Install playwright locally in the app directory to ensure it's found
WORKDIR /app
RUN npm install playwright

# Move into the project folder
WORKDIR /src/ValuationService/src/ValuationService

# Restore
RUN dotnet restore "ValuationService.csproj" --configfile /src/NuGet.config

# Publish
RUN dotnet publish "ValuationService.csproj" -c Release -o /out

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /out .
COPY --from=build /app/research_comicbookrealm.js .
COPY --from=build /app/node_modules ./node_modules

# Install Playwright browsers
# Note: npx playwright is usually available via dotnet tool or npm, 
# but if we're using a .NET project, we might need to ensure nodejs/npm is available 
# OR use the playwright dotnet tool if it's integrated.
# The issue description says: RUN npx playwright install --with-deps chromium
# So we need nodejs in the final image or build image. 
# Let's add nodejs to base for playwright installation.

USER root
RUN apt-get update && apt-get install -y curl gnupg \
    && curl -sL https://deb.nodesource.com/setup_18.x | bash - \
    && apt-get install -y nodejs \
    && npx playwright install --with-deps chromium \
    && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "ValuationService.dll"]
