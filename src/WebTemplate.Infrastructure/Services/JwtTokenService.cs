using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebTemplate.Domain.Entities;
using WebTemplate.Domain.Interfaces;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using WebTemplate.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace WebTemplate.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public JwtTokenService(
        IOptions<JwtSettings> jwtSettings,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _jwtSettings = jwtSettings.Value;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public (string Token, DateTime Expires) GenerateToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.UserRoles != null && user.UserRoles.Any())
        {
            foreach (var userRole in user.UserRoles)
            {
                if (userRole.Role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string Token, DateTime Expires) GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);
        var expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        return (token, expires);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(User user, string? ipAddress = null)
    {
        var (token, expires) = GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = user.Id,
            ExpiresAt = expires,
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);
        return refreshToken;
    }

    public async Task<(User User, RefreshToken NewRefreshToken)?> RefreshTokenAsync(string token, string? ipAddress = null)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null)
        {
            return null;
        }

        if (!refreshToken.IsActive)
        {
            return null;
        }

        // Revoke the old token
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        // Create new refresh token (rotation)
        var (newToken, expires) = GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newToken,
            UserId = refreshToken.UserId,
            ExpiresAt = expires,
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        refreshToken.ReplacedByToken = newToken;

        await _refreshTokenRepository.UpdateAsync(refreshToken);
        await _refreshTokenRepository.CreateAsync(newRefreshToken);

        newRefreshToken.User = refreshToken.User;

        return (refreshToken.User, newRefreshToken);
    }

    public async Task RevokeTokenAsync(string token, string? ipAddress = null)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return;
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        await _refreshTokenRepository.UpdateAsync(refreshToken);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress = null)
    {
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, ipAddress);
    }
}
