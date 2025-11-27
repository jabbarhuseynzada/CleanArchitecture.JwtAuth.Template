# WebTemplate Setup Verification Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WebTemplate Configuration Checker" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check password consistency
Write-Host "[1] Checking password consistency..." -ForegroundColor Yellow
$pass1 = (Select-String -Path "docker-compose.yml" -Pattern "postgres123").Matches.Count
$pass2 = (Select-String -Path "docker-compose.postgres.yml" -Pattern "postgres123").Matches.Count
$pass3 = (Select-String -Path "docker-compose.api-only.yml" -Pattern "postgres123").Matches.Count
$pass4 = (Select-String -Path "src/WebTemplate.WebApi/.env" -Pattern "postgres123").Matches.Count

if ($pass1 -gt 0 -and $pass2 -gt 0 -and $pass3 -gt 0 -and $pass4 -gt 0) {
    Write-Host "    ✓ All passwords are consistent (postgres123)`n" -ForegroundColor Green
} else {
    Write-Host "    ✗ Password mismatch detected!`n" -ForegroundColor Red
}

# Check automatic migrations
Write-Host "[2] Checking automatic migrations..." -ForegroundColor Yellow
$migrations = Select-String -Path "src/WebTemplate.WebApi/Program.cs" -Pattern "await context.Database.MigrateAsync" | Where-Object { $_.Line -notmatch "^\s*//" }
if ($migrations) {
    Write-Host "    ✓ Automatic migrations enabled`n" -ForegroundColor Green
} else {
    Write-Host "    ⚠ Automatic migrations commented out`n" -ForegroundColor Yellow
}

# Check Docker files
Write-Host "[3] Checking Docker Compose files..." -ForegroundColor Yellow
$files = @("docker-compose.yml", "docker-compose.postgres.yml", "docker-compose.api-only.yml")
$allExist = $true
foreach ($f in $files) {
    if (Test-Path $f) {
        Write-Host "    ✓ $f exists" -ForegroundColor Green
    } else {
        Write-Host "    ✗ $f missing" -ForegroundColor Red
        $allExist = $false
    }
}
Write-Host ""

# Check Docker
Write-Host "[4] Checking Docker..." -ForegroundColor Yellow
try {
    $version = docker --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    ✓ Docker installed: $version" -ForegroundColor Green
        $ps = docker ps 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "    ✓ Docker is running`n" -ForegroundColor Green
        } else {
            Write-Host "    ⚠ Docker installed but not running`n" -ForegroundColor Yellow
        }
    } else {
        Write-Host "    ✗ Docker not found`n" -ForegroundColor Red
    }
} catch {
    Write-Host "    ✗ Docker not found`n" -ForegroundColor Red
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Quick Start Commands:" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
Write-Host "Full stack:" -ForegroundColor Yellow
Write-Host "  docker-compose up -d`n" -ForegroundColor White
Write-Host "Database only (for local dev):" -ForegroundColor Yellow
Write-Host "  docker-compose -f docker-compose.postgres.yml up -d" -ForegroundColor White
Write-Host "  cd src/WebTemplate.WebApi && dotnet run`n" -ForegroundColor White
Write-Host "Documentation:" -ForegroundColor Yellow
Write-Host "  - README.md" -ForegroundColor White
Write-Host "  - DOCKER-GUIDE.md" -ForegroundColor White
Write-Host "  - CHANGELOG.md`n" -ForegroundColor White
