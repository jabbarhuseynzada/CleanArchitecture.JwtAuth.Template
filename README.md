# Clean Architecture Web API Template with JWT Authentication

A production-ready .NET 9 Web API template with Clean Architecture, JWT Authentication, CQRS pattern, and enterprise features.

## Technologies & Libraries

| Category | Technology |
|----------|------------|
| **Framework** | .NET 9, ASP.NET Core Web API |
| **Architecture** | Clean Architecture, CQRS Pattern |
| **Authentication** | JWT Bearer Tokens, Refresh Tokens, BCrypt |
| **Database** | PostgreSQL, Entity Framework Core |
| **Mediator** | MediatR |
| **Validation** | FluentValidation |
| **Logging** | Serilog (Console + File) |
| **Caching** | Redis / In-Memory |
| **Background Jobs** | Hangfire |
| **API Docs** | Swagger / OpenAPI |
| **Observability** | OpenTelemetry |
| **Containerization** | Docker, Docker Compose |

---

## Installation

```bash
dotnet new install CleanArchitecture.JwtAuth.Template
```

## Create New Project

```bash
dotnet new cleanapi -n YourProjectName
```

With parameters:
```bash
dotnet new cleanapi -n MyApp \
  --UseDocker true \
  --PostgresPassword "SecurePass123" \
  --JwtSecretKey "your-32-character-secret-key"
```

---

## Project Structure

```
src/
├── YourProjectName.Domain/           # Entities, Interfaces (no dependencies)
├── YourProjectName.Application/      # DTOs, Commands, Handlers, Validators
├── YourProjectName.Infrastructure/   # Database, Repositories, Services
└── YourProjectName.WebApi/           # Controllers, Middleware, Program.cs
```

---

## Getting Started

### Option 1: Run Everything with Docker (Recommended)

The easiest way to get started - migrations and database seeding happen automatically:

```bash
# Start database + API in Docker
docker-compose up -d

# View logs
docker-compose logs -f webtemplate-api
```

### Option 2: Run Database in Docker + API Locally

This is better for development since you can debug the API:

```bash
# Start PostgreSQL + PgAdmin
docker-compose -f docker-compose.postgres.yml up -d

# Run the app (migrations and seeding happen automatically on startup)
cd src/YourProjectName.WebApi
dotnet run
```

**Note**: Automatic migrations are enabled in `Program.cs` line 324. The database tables and admin user will be created automatically on first run.
  **Create migration if missing**:
   ```bash
   cd src/YourProjectName.WebApi
   dotnet ef migrations add InitialCreate -p ../YourProjectName.Infrastructure
   ```

### Accessible URLs:
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger
- **PgAdmin**: http://localhost:5050 (admin@webtemplate.com / admin123)

### Default Admin Account:
- **Username**: admin
- **Password**: Admin@123
- **Email**: admin@webtemplate.com

---

## Adding New Features

### Step 1: Create Entity (Domain Layer)

```csharp
// src/YourProjectName.Domain/Entities/Product.cs
using YourProjectName.Domain.Common;

namespace YourProjectName.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}
```

### Step 2: Create Repository Interface (Domain Layer)

```csharp
// src/YourProjectName.Domain/Interfaces/RepositoryContracts/IProductRepository.cs
using YourProjectName.Domain.Entities;

namespace YourProjectName.Domain.Interfaces.RepositoryContracts;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
}
```

### Step 3: Implement Repository (Infrastructure Layer)

```csharp
// src/YourProjectName.Infrastructure/Repository/ProductRepository.cs
using YourProjectName.Domain.Entities;
using YourProjectName.Domain.Interfaces.RepositoryContracts;
using YourProjectName.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace YourProjectName.Infrastructure.Repository;

public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<Product>> GetAllAsync()
        => await _context.Products.Where(p => !p.IsDeleted).ToListAsync();

    public async Task<Product?> GetByIdAsync(Guid id)
        => await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            product.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}
```

### Step 4: Register in DbContext & DI

```csharp
// In ApplicationDbContext.cs
public DbSet<Product> Products => Set<Product>();

// In Infrastructure/DependencyInjection.cs
services.AddScoped<IProductRepository, ProductRepository>();
```

### Step 5: Create DTOs (Application Layer)

```csharp
// src/YourProjectName.Application/DTOs/Product/CreateProductRequest.cs
namespace YourProjectName.Application.DTOs.Product;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
}

// src/YourProjectName.Application/DTOs/Product/ProductResponse.cs
namespace YourProjectName.Application.DTOs.Product;

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Step 6: Create Command (Application Layer)

```csharp
// src/YourProjectName.Application/Features/Products/Commands/CreateProduct/CreateProductCommand.cs
using YourProjectName.Application.DTOs.Product;
using MediatR;

namespace YourProjectName.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId
) : IRequest<ProductResponse>;
```

### Step 7: Create Handler (Application Layer)

```csharp
// src/YourProjectName.Application/Features/Products/Handlers/CreateProduct/CreateProductCommandHandler.cs
using YourProjectName.Application.DTOs.Product;
using YourProjectName.Application.Features.Products.Commands.CreateProduct;
using YourProjectName.Domain.Entities;
using YourProjectName.Domain.Interfaces.RepositoryContracts;
using MediatR;

namespace YourProjectName.Application.Features.Products.Handlers.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId
        };

        await _productRepository.AddAsync(product);

        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt
        };
    }
}
```

### Step 8: Create Validator (Application Layer)

```csharp
// src/YourProjectName.Application/Validators/CreateProductRequestValidator.cs
using FluentValidation;
using YourProjectName.Application.DTOs.Product;

namespace YourProjectName.Application.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0);
    }
}
```

### Step 9: Create Controller (WebApi Layer)

```csharp
// src/YourProjectName.WebApi/Controllers/ProductsController.cs
using Asp.Versioning;
using YourProjectName.Application.DTOs.Product;
using YourProjectName.Application.Features.Products.Commands.CreateProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace YourProjectName.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[EnableRateLimiting("per-ip")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.Stock,
            request.CategoryId
        );

        var product = await _mediator.Send(command);
        return CreatedAtAction(nameof(Create), new { id = product.Id }, product);
    }
}
```

### Step 10: Run Migration

```bash
cd src/YourProjectName.WebApi
dotnet ef migrations add AddProduct -p ../YourProjectName.Infrastructure
dotnet ef database update
```

---

## Feature Checklist

When adding a new feature, follow this order:

| # | Layer | Task |
|---|-------|------|
| 1 | Domain | Create Entity in `Entities/` |
| 2 | Domain | Create Repository Interface in `Interfaces/RepositoryContracts/` |
| 3 | Infrastructure | Implement Repository in `Repository/` |
| 4 | Infrastructure | Add DbSet to `ApplicationDbContext` |
| 5 | Infrastructure | Register Repository in `DependencyInjection.cs` |
| 6 | Application | Create DTOs in `DTOs/` |
| 7 | Application | Create Command/Query in `Features/{Feature}/Commands/` |
| 8 | Application | Create Handler in `Features/{Feature}/Handlers/` |
| 9 | Application | Create Validator in `Validators/` |
| 10 | WebApi | Create Controller in `Controllers/` |
| 11 | - | Run `dotnet ef migrations add` and `dotnet ef database update` |

---

## Environment Variables

| Variable | Description |
|----------|-------------|
| `CONNECTION_STRING` | PostgreSQL connection string |
| `JWT_SECRET_KEY` | 32+ character secret key |
| `JWT_EXPIRY_MINUTES` | Token expiration (default: 60) |
| `ENABLE_HANGFIRE` | Enable background jobs |
| `REDIS_CONNECTION_STRING` | Redis for caching (optional) |

---

## Troubleshooting

### Database Connection Issues

If you encounter "password authentication failed" errors:

1. **Check password consistency**: Ensure all configuration files use the same password:
   - `.env` file: `Password=postgres123`
   - `docker-compose.yml`: `POSTGRES_PASSWORD=postgres123`
   - `docker-compose.postgres.yml`: `POSTGRES_PASSWORD=postgres123`
   - `docker-compose.api-only.yml`: `Password=postgres123`

2. **Recreate database with fresh password**:
   ```bash
   # Stop and remove all containers and volumes
   docker-compose down -v

   # Start fresh
   docker-compose up -d
   ```

### Migration Issues

If tables are not being created:

1. **Verify automatic migrations are enabled** in `src/YourProjectName.WebApi/Program.cs` (line 324):
   ```csharp
   await context.Database.MigrateAsync(); // Should NOT be commented
   ```

2. **Check if migrations exist**:
   ```bash
   ls src/YourProjectName.Infrastructure/Migrations/
   ```

3. **Create migration if missing**:
   ```bash
   cd src/YourProjectName.WebApi
   dotnet ef migrations add InitialCreate -p ../YourProjectName.Infrastructure
   ```

### Port Already in Use

If port 5000 or 5432 is already in use:

```bash
# Find and kill the process using the port (Windows)
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Or change ports in docker-compose.yml
ports:
  - "5100:8080"  # Change 5000 to 5100
  - "5433:5432"  # Change 5432 to 5433
```

### Clean Start

For a completely fresh start:

```bash
# Remove all containers, volumes, and images
docker-compose down -v --rmi all

# Rebuild and start
docker-compose up -d --build
```

---

## License

MIT License
