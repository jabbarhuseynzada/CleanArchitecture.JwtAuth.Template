using WebTemplate.Domain.Entities;
using WebTemplate.Domain.Interfaces;
using WebTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace WebTemplate.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    Description = "Administrator with full access",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    Description = "Regular user with basic access",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedAdminUserAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@webtemplate.com";

        if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                throw new InvalidOperationException("Admin role not found. Please seed roles first.");
            }

            var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123";
            var adminFirstName = Environment.GetEnvironmentVariable("ADMIN_FIRSTNAME") ?? "System";
            var adminLastName = Environment.GetEnvironmentVariable("ADMIN_LASTNAME") ?? "Administrator";

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = adminUsername,
                FirstName = adminFirstName,
                LastName = adminLastName,
                Email = adminEmail,
                Password = passwordHasher.HashPassword(adminPassword),
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();

            var userRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            };

            await context.UserRoles.AddAsync(userRole);
            await context.SaveChangesAsync();
        }
    }
}
