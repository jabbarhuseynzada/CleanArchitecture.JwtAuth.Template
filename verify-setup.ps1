# WebTemplate Setup Verification Script
# Run this after installing the template to verify everything is configured correctly

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WebTemplate Configuration Checker" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$issues = 0
$warnings = 0

# Check 1: Password Consistency
Write-Host "[1/6] Checking password consistency..." -ForegroundColor Yellow

$passwords = @{
    "docker-compose.yml" = (Select-String -Path "docker-compose.yml" -Pattern "POSTGRES_PASSWORD=" | Select-Object -First 1)
    "docker-compose.postgres.yml" = (Select-String -Path "docker-compose.postgres.yml" -Pattern "POSTGRES_PASSWORD:" | Select-Object -First 1)
    "docker-compose.api-only.yml" = (Select-String -Path "docker-compose.api-only.yml" -Pattern "Password=" | Select-Object -First 1)
    ".env" = (Select-String -Path "src/WebTemplate.WebApi/.env" -Pattern "Password=" | Select-Object -First 1)
}

$passwordValues = @()
foreach ($file in $passwords.Keys) {
    if ($passwords[$file]) {
        $line = $passwords[$file].Line
        if ($line -match "Password[=:](.+?)([;`"'\s]|$)") {
            $passwordValues += $matches[1].Trim()
        }
    }
}

$uniquePasswords = $passwordValues | Select-Object -Unique
if ($uniquePasswords.Count -eq 1) {
    Write-Host "   ✓ All passwords are consistent: $($uniquePasswords[0])" -ForegroundColor Green
} else {
    Write-Host "   ✗ Password mismatch detected!" -ForegroundColor Red
    Write-Host "     Found: $($uniquePasswords -join ', ')" -ForegroundColor Red
    $issues++
}

# Check 2: Automatic Migrations
Write-Host "[2/6] Checking automatic migrations..." -ForegroundColor Yellow

$programCs = Get-Content "src/WebTemplate.WebApi/Program.cs" -Raw
if ($programCs -match "^\s*await context\.Database\.MigrateAsync\(\);.*$" -and $programCs -notmatch "^\s*//.*await context\.Database\.MigrateAsync\(\);.*$") {
    Write-Host "   ✓ Automatic migrations enabled" -ForegroundColor Green
} else {
    Write-Host "   ⚠ Automatic migrations may be commented out" -ForegroundColor Yellow
    Write-Host "     Check Program.cs line 324" -ForegroundColor Yellow
    $warnings++
}

# Check 3: Docker Compose Files Exist
Write-Host "[3/6] Checking Docker Compose files..." -ForegroundColor Yellow

$dockerFiles = @(
    "docker-compose.yml",
    "docker-compose.postgres.yml",
    "docker-compose.api-only.yml"
)

foreach ($file in $dockerFiles) {
    if (Test-Path $file) {
        Write-Host "   ✓ $file exists" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $file missing" -ForegroundColor Red
        $issues++
    }
}

# Check 4: .env File
Write-Host "[4/6] Checking .env file..." -ForegroundColor Yellow

if (Test-Path "src/WebTemplate.WebApi/.env") {
    Write-Host "   ✓ .env file exists" -ForegroundColor Green

    # Check required variables
    $envContent = Get-Content "src/WebTemplate.WebApi/.env" -Raw
    $requiredVars = @("CONNECTION_STRING", "JWT_SECRET_KEY", "ADMIN_USERNAME", "ADMIN_PASSWORD")

    foreach ($var in $requiredVars) {
        if ($envContent -match "$var=") {
            Write-Host "   ✓ $var configured" -ForegroundColor Green
        } else {
            Write-Host "   ✗ $var missing" -ForegroundColor Red
            $issues++
        }
    }
} else {
    Write-Host "   ✗ .env file missing" -ForegroundColor Red
    $issues++
}

# Check 5: Migrations Folder
Write-Host "[5/6] Checking migrations..." -ForegroundColor Yellow

if (Test-Path "src/WebTemplate.Infrastructure/Migrations") {
    $migrations = Get-ChildItem "src/WebTemplate.Infrastructure/Migrations" -Filter "*.cs" | Where-Object { $_.Name -notmatch "ModelSnapshot" }
    if ($migrations.Count -gt 0) {
        Write-Host "   ✓ Found $($migrations.Count) migration(s)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠ No migrations found" -ForegroundColor Yellow
        Write-Host "     Run: dotnet ef migrations add InitialCreate" -ForegroundColor Yellow
        $warnings++
    }
} else {
    Write-Host "   ⚠ Migrations folder doesn't exist yet" -ForegroundColor Yellow
    $warnings++
}

# Check 6: Docker Availability
Write-Host "[6/6] Checking Docker..." -ForegroundColor Yellow

try {
    $dockerVersion = docker --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ Docker is installed: $dockerVersion" -ForegroundColor Green

        # Check if Docker is running
        $dockerPs = docker ps 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ Docker is running" -ForegroundColor Green
        } else {
            Write-Host "   ⚠ Docker is installed but not running" -ForegroundColor Yellow
            Write-Host "     Start Docker Desktop" -ForegroundColor Yellow
            $warnings++
        }
    } else {
        Write-Host "   ✗ Docker not found" -ForegroundColor Red
        $issues++
    }
} catch {
    Write-Host "   ✗ Docker not found" -ForegroundColor Red
    $issues++
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Verification Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($issues -eq 0 -and $warnings -eq 0) {
    Write-Host "✓ All checks passed! Your project is ready to run." -ForegroundColor Green
    Write-Host ""
    Write-Host "Quick Start:" -ForegroundColor Cyan
    Write-Host "  docker-compose up -d" -ForegroundColor White
    Write-Host "  docker-compose logs -f webtemplate-api" -ForegroundColor White
} elseif ($issues -eq 0) {
    Write-Host "⚠ $warnings warning(s) detected, but project should work." -ForegroundColor Yellow
} else {
    Write-Host "✗ $issues issue(s) and $warnings warning(s) detected." -ForegroundColor Red
    Write-Host "  Please fix the issues above before running." -ForegroundColor Red
}

Write-Host ""
Write-Host "Documentation:" -ForegroundColor Cyan
Write-Host "  - README.md - Getting started guide" -ForegroundColor White
Write-Host "  - DOCKER-GUIDE.md - Docker configuration reference" -ForegroundColor White
Write-Host "  - CHANGELOG.md - Recent improvements" -ForegroundColor White
Write-Host ""
