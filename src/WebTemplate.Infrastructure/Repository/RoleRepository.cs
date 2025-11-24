using WebTemplate.Domain.Entities;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using WebTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace WebTemplate.Infrastructure.Repository;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        var value = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName && !r.IsDeleted);
        return value;
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        var values = await _context.Roles
            .Where(r => !r.IsDeleted)
            .ToListAsync();
        return values;
    }
}
