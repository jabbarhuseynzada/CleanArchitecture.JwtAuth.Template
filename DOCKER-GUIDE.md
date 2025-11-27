# Docker Compose Guide

This project includes three Docker Compose configurations for different use cases.

## Quick Reference

| File | Purpose | When to Use | Command |
|------|---------|-------------|---------|
| `docker-compose.yml` | Full stack (API + Database) | Production, Testing, Quick Demo | `docker-compose up -d` |
| `docker-compose.postgres.yml` | Database + PgAdmin only | Development (run API locally) | `docker-compose -f docker-compose.postgres.yml up -d` |
| `docker-compose.api-only.yml` | API only | API in container, DB on host | `docker-compose -f docker-compose.api-only.yml up -d` |

---

## 1. docker-compose.yml (Full Stack)

**What it does:**
- Starts PostgreSQL database
- Builds and runs the API
- Applies migrations automatically
- Seeds admin user and roles

**Services:**
- `webtemplate-api` - The .NET API (ports 5000, 5001)
- `webtemplate-db` - PostgreSQL database (port 5432)

**Use this when:**
- ✅ You want everything running in Docker
- ✅ Testing or demonstrating the full application
- ✅ You want automatic database setup
- ✅ Deploying to production

**Commands:**
```bash
# Start
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down

# Stop and remove volumes (fresh start)
docker-compose down -v
```

**Accessible at:**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health

---

## 2. docker-compose.postgres.yml (Database Only)

**What it does:**
- Starts PostgreSQL database
- Starts PgAdmin web interface
- Allows you to run the API from Visual Studio/VS Code

**Services:**
- `postgres` - PostgreSQL database (port 5432)
- `pgadmin` - Database management UI (port 5050)

**Use this when:**
- ✅ Developing the API locally (debugging with Visual Studio/Rider/VS Code)
- ✅ Running migrations manually
- ✅ Need database access while coding

**Commands:**
```bash
# Start
docker-compose -f docker-compose.postgres.yml up -d

# Stop
docker-compose -f docker-compose.postgres.yml down

# Fresh start
docker-compose -f docker-compose.postgres.yml down -v
docker-compose -f docker-compose.postgres.yml up -d
```

**Then run the API locally:**
```bash
cd src/WebTemplate.WebApi
dotnet run
```

**Accessible at:**
- PgAdmin: http://localhost:5050
  - Email: admin@webtemplate.com
  - Password: admin123
- PostgreSQL: localhost:5432
  - Username: postgres
  - Password: postgres123

**PgAdmin Server Connection:**
1. Open http://localhost:5050
2. Right-click "Servers" → "Register" → "Server"
3. General Tab:
   - Name: WebTemplate
4. Connection Tab:
   - Host: postgres
   - Port: 5432
   - Username: postgres
   - Password: postgres123

---

## 3. docker-compose.api-only.yml (API Only)

**What it does:**
- Runs only the API in Docker
- Connects to PostgreSQL running on your host machine

**Services:**
- `webtemplate-api` - The .NET API (ports 5000, 5001)

**Use this when:**
- ✅ You have PostgreSQL installed on your machine
- ✅ Testing API containerization
- ✅ Database is running elsewhere (cloud, another server)

**Requirements:**
- PostgreSQL must be running on your host machine
- Update connection string in the file if needed

**Commands:**
```bash
# Start
docker-compose -f docker-compose.api-only.yml up -d

# View logs
docker-compose -f docker-compose.api-only.yml logs -f

# Stop
docker-compose -f docker-compose.api-only.yml down
```

**Note:** The connection string uses `host.docker.internal` to connect to the host machine's PostgreSQL.

---

## Common Tasks

### View Container Logs
```bash
# All containers
docker-compose logs -f

# Specific container
docker logs webtemplate-api -f
docker logs webtemplate-db -f
```

### Restart Containers
```bash
# Restart all
docker-compose restart

# Restart specific service
docker-compose restart webtemplate-api
```

### Rebuild After Code Changes
```bash
# Rebuild and restart
docker-compose up -d --build
```

### Check Container Status
```bash
docker-compose ps
```

### Access Database from Container
```bash
# Execute psql inside the container
docker exec -it webtemplate-db psql -U postgres -d WebTemplateDb

# List tables
docker exec webtemplate-db psql -U postgres -d WebTemplateDb -c "\dt"

# Query users
docker exec webtemplate-db psql -U postgres -d WebTemplateDb -c "SELECT * FROM \"Users\";"
```

### Completely Clean Environment
```bash
# Remove everything (containers, volumes, networks, images)
docker-compose down -v --rmi all

# Start fresh
docker-compose up -d --build
```

---

## Environment Variables

All environment variables can be configured in:
1. `.env` file (for local development)
2. `docker-compose.yml` (for Docker deployment)

**Critical Variables:**
- `CONNECTION_STRING` - Database connection
- `JWT_SECRET_KEY` - Must be 32+ characters
- `POSTGRES_PASSWORD` - **Must match** across all files

**Ensure password consistency:**
```bash
# Check passwords in all files
grep -r "postgres123" docker-compose*.yml src/WebTemplate.WebApi/.env
```

---

## Troubleshooting

### Port Already in Use

```bash
# Windows: Find process using port 5000
netstat -ano | findstr :5000

# Kill process
taskkill /PID <PID> /F

# Or change port in docker-compose.yml
ports:
  - "5100:8080"  # Changed from 5000
```

### Cannot Connect to Database

1. Check if container is running:
   ```bash
   docker ps | grep postgres
   ```

2. Check container logs:
   ```bash
   docker logs webtemplate-db
   ```

3. Test connection:
   ```bash
   docker exec webtemplate-db pg_isready -U postgres
   ```

### Migrations Not Applied

If you're using `docker-compose.postgres.yml` and running API locally:

```bash
cd src/WebTemplate.WebApi
dotnet ef database update -p ../WebTemplate.Infrastructure
```

Or enable automatic migrations in `Program.cs` line 324.

---

## Best Practices

1. **Development**: Use `docker-compose.postgres.yml` and run API locally for better debugging
2. **Testing**: Use `docker-compose.yml` to test the full containerized stack
3. **Production**: Use `docker-compose.yml` with environment-specific configuration
4. **Always use volumes**: Don't lose data when containers restart
5. **Check logs**: Always check logs when something doesn't work
6. **Fresh start**: Use `down -v` to start completely fresh when troubleshooting

---

## Quick Start Cheat Sheet

```bash
# Full stack (easiest)
docker-compose up -d

# Database only (for local development)
docker-compose -f docker-compose.postgres.yml up -d
cd src/WebTemplate.WebApi && dotnet run

# View logs
docker-compose logs -f

# Stop everything
docker-compose down

# Complete clean and restart
docker-compose down -v
docker-compose up -d --build
```
