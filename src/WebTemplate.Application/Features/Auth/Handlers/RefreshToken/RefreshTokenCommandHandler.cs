using WebTemplate.Application.DTOs.Auth;
using WebTemplate.Application.Features.Auth.Commands.RefreshToken;
using WebTemplate.Domain.Interfaces;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Handlers.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken, request.IpAddress);

        if (result == null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        var (user, newRefreshToken) = result.Value;

        var (token, tokenExpires) = _jwtTokenService.GenerateToken(user);

        var roles = user.UserRoles?
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList() ?? new List<string>();

        return new AuthResponse
        {
            Token = token,
            TokenExpires = tokenExpires,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpires = newRefreshToken.ExpiresAt,
            Email = user.Email,
            UserName = user.UserName,
            UserId = user.Id,
            Roles = roles
        };
    }
}
