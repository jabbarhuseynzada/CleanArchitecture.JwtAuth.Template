using WebTemplate.Domain.Entities;

namespace WebTemplate.Domain.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime Expires) GenerateToken(User user);
    (string Token, DateTime Expires) GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(User user, string? ipAddress = null);
    Task<(User User, RefreshToken NewRefreshToken)?> RefreshTokenAsync(string token, string? ipAddress = null);
    Task RevokeTokenAsync(string token, string? ipAddress = null);
    Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress = null);
}
