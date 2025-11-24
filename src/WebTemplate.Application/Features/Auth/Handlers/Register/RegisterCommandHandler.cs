using WebTemplate.Application.DTOs.Auth;
using WebTemplate.Application.Features.Auth.Commands.Register;
using WebTemplate.Domain.Entities;
using WebTemplate.Domain.Interfaces;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Handlers.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.UserExistsByEmailAsync(request.Email);
        if (existingUser)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var userRole = await _roleRepository.GetRoleByNameAsync("User");
        if (userRole == null)
        {
            throw new InvalidOperationException("Default 'User' role not found. Please seed the database.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = _passwordHasher.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = Guid.NewGuid(),
                    RoleId = userRole.Id,
                    Role = userRole
                }
            }
        };

        user.UserRoles.First().UserId = user.Id;

        await _userRepository.AddUserAsync(user);

        var (token, tokenExpires) = _jwtTokenService.GenerateToken(user);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user);

        return new AuthResponse
        {
            Token = token,
            TokenExpires = tokenExpires,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpires = refreshToken.ExpiresAt,
            Email = user.Email,
            UserName = user.UserName,
            UserId = user.Id,
            Roles = new List<string> { userRole.Name }
        };
    }
}
