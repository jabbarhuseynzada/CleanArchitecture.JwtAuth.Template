using WebTemplate.Domain.Entities;

namespace WebTemplate.Domain.Interfaces.RepositoryContracts;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null);
    Task DeleteExpiredTokensAsync();
}
