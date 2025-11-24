using WebTemplate.Domain.Entities;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using WebTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace WebTemplate.Infrastructure.Repository;

public class UserRepository(ApplicationDbContext applicationDbContext) : IUserRepository
{
    private readonly ApplicationDbContext _context = applicationDbContext;

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.IsDeleted = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException("User not found.");
        }
    }

    public async Task UpdateUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var values = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted)
            .ToListAsync();
        return values;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        var value = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        return value;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var value = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        return value;
    }

    public async Task<User?> GetUserWithRolesByEmailAsync(string email)
    {
        var value = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        return value;
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        var value = await _context.Users
            .AnyAsync(u => u.Email == email && !u.IsDeleted);
        return value;
    }
}
