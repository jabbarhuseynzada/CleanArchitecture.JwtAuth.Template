using WebTemplate.Domain.Entities;

namespace WebTemplate.Domain.Interfaces.RepositoryContracts;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByUserIdAndCodeAsync(Guid userId, string code);
    Task<PasswordResetToken?> GetValidTokenByEmailAndCodeAsync(string email, string code);
    Task AddAsync(PasswordResetToken token);
    Task UpdateAsync(PasswordResetToken token);
    Task InvalidateUserTokensAsync(Guid userId);
}
