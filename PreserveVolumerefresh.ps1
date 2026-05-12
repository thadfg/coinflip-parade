$composeFile = "docker-compose.local.yml"

# 1. Stop existing containers but KEEP volumes/data
Write-Host "Stopping containers..." -ForegroundColor Cyan
docker compose -f $composeFile down

# 2. Build images using cache where possible for speed
# Use --no-cache only if you are troubleshooting a specific build issue
Write-Host "Starting build..." -ForegroundColor Cyan
docker compose -f $composeFile build

# 3. Check if the build command succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Starting containers..." -ForegroundColor Green
    
    # 4. Bring the stack up
    docker compose -f $composeFile up -d
    
    Write-Host "Deployment complete!" -ForegroundColor Green
} else {
    Write-Host "Build failed! Deployment aborted." -ForegroundColor Red
    exit $LASTEXITCODE
}