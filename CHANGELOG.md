# Changelog

## [Unreleased] - 2025-11-27

### Fixed
- **Password Configuration Consistency**: Fixed password mismatch in `docker-compose.api-only.yml`
  - Changed `Password=admin` to `Password=postgres123` to match other configuration files
  - Ensures consistent authentication across all deployment scenarios

- **Automatic Migrations**: Enabled automatic database migrations in `Program.cs`
  - Uncommented `await context.Database.MigrateAsync();` on line 324
  - Database tables now created automatically on application startup
  - No manual migration commands required for initial setup

### Added
- **Docker Compose Guide**: Created comprehensive `DOCKER-GUIDE.md` documentation
  - Explains all three Docker Compose configurations
  - Includes common tasks and troubleshooting steps
  - Quick reference table for choosing the right configuration

### Improved
- **README.md**: Updated Getting Started section
  - Clarified two recommended approaches (full Docker vs local development)
  - Added accessible URLs and default credentials
  - Included troubleshooting section for common issues
  - Better organized with clear instructions

### Documentation
- Added troubleshooting section to README for:
  - Database connection issues
  - Migration problems
  - Port conflicts
  - Clean environment setup

## Why These Changes Were Made

### Problem 1: Password Authentication Failed
**Issue**: The `docker-compose.api-only.yml` file had `Password=admin` while all other files used `postgres123`. This caused authentication failures when trying to run the API container with a host database.

**Solution**: Updated to use consistent password `postgres123` across all configuration files.

### Problem 2: Database Tables Not Created
**Issue**: Automatic migrations were commented out in `Program.cs`. New users had to manually run EF Core migration commands, which was confusing and error-prone.

**Solution**: Enabled automatic migrations so database schema is created on first run, improving the "out of the box" experience.

### Problem 3: Unclear Setup Instructions
**Issue**: README didn't clearly explain when to use each Docker Compose file or how to get started quickly.

**Solution**:
- Restructured Getting Started section with two clear options
- Created dedicated DOCKER-GUIDE.md for comprehensive Docker documentation
- Added troubleshooting section for common issues

## Migration Guide for Existing Projects

If you created a project from this template before these fixes:

### 1. Update Password Consistency
Check and update all password references to use the same value:

```bash
# Check current passwords
grep -r "Password=" docker-compose*.yml src/*/WebApi/.env

# Update docker-compose.api-only.yml if needed
# Change Password=admin to Password=postgres123
```

### 2. Enable Automatic Migrations
Edit `src/YourProject.WebApi/Program.cs` around line 324:

```csharp
// Before:
// await context.Database.MigrateAsync();

// After:
await context.Database.MigrateAsync();
```

### 3. Test the Changes
```bash
# Clean start
docker-compose down -v

# Start fresh
docker-compose up -d

# Check logs
docker-compose logs -f

# Verify database tables created
docker exec webtemplate-db psql -U postgres -d WebTemplateDb -c "\dt"
```

## Template Parameter Defaults

For future template instantiation, these are the recommended defaults:

```bash
dotnet new cleanapi -n MyProject \
  --UseDocker true \
  --PostgresPassword "postgres123" \
  --JwtSecretKey "your-32-character-secret-key-here" \
  --EnableAutoMigrations true
```

## Breaking Changes
None - these are backward-compatible improvements.

## Notes for Template Maintainers

These fixes should be incorporated into the template source code:

1. Update `.template.config/template.json` to set `EnableAutoMigrations` default to `true`
2. Ensure all password placeholders use the same parameter variable
3. Include `DOCKER-GUIDE.md` in template output
4. Update README.md with improved Getting Started section
