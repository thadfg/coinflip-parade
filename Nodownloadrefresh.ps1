# 1. Bring down containers and remove volumes (keeping images/cache)
docker compose -f docker-compose.local.yml down -v

# 2. Build only what has changed (utilizing cache)
docker compose -f docker-compose.local.yml build

# 3. Start services
docker compose -f docker-compose.local.yml up -d

# 4. Clean up dangling build data (non-aggressive)
docker builder prune -f

Write-Host "Deployment complete!" -ForegroundColor Green