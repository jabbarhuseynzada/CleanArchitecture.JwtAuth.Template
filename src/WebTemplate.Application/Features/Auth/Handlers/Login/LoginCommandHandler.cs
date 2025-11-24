using WebTemplate.Application.DTOs.Auth;
using WebTemplate.Application.Features.Auth.Commands.Login;
using WebTemplate.Domain.Interfaces;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Handlers.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserWithRolesByEmailAsync(request.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var (token, tokenExpires) = _jwtTokenService.GenerateToken(user);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user, request.IpAddress);

        var roles = user.UserRoles?
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList() ?? new List<string>();

        return new AuthResponse
        {
            Token = token,
            TokenExpires = tokenExpires,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpires = refreshToken.ExpiresAt,
            Email = user.Email,
            UserName = user.UserName,
            UserId = user.Id,
            Roles = roles
        };
    }
}
