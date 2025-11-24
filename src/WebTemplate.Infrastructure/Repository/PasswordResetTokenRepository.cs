using WebTemplate.Domain.Entities;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using WebTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace WebTemplate.Infrastructure.Repository;

public class PasswordResetTokenRepository(ApplicationDbContext applicationDbContext) : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context = applicationDbContext;

    public async Task<PasswordResetToken?> GetByUserIdAndCodeAsync(Guid userId, string code)
    {
        return await _context.Set<PasswordResetToken>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Code == code && !t.IsDeleted);
    }

    public async Task<PasswordResetToken?> GetValidTokenByEmailAndCodeAsync(string email, string code)
    {
        return await _context.Set<PasswordResetToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.User.Email == email &&
                t.Code == code &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow &&
                !t.IsDeleted);
    }

    public async Task AddAsync(PasswordResetToken token)
    {
        await _context.Set<PasswordResetToken>().AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PasswordResetToken token)
    {
        _context.Set<PasswordResetToken>().Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task InvalidateUserTokensAsync(Guid userId)
    {
        var tokens = await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId && !t.IsUsed && !t.IsDeleted)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsUsed = true;
            token.ModifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
