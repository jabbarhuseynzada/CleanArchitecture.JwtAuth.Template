using WebTemplate.Application.DTOs.Auth;
using WebTemplate.Application.Features.Auth.Commands.VerifyResetCode;
using WebTemplate.Domain.Interfaces;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Handlers.VerifyResetCode;

public class VerifyResetCodeCommandHandler : IRequestHandler<VerifyResetCodeCommand, ResetPasswordResponse>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public VerifyResetCodeCommandHandler(
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ResetPasswordResponse> Handle(VerifyResetCodeCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _passwordResetTokenRepository.GetValidTokenByEmailAndCodeAsync(request.Email, request.Code);

        if (resetToken == null)
        {
            throw new UnauthorizedAccessException("Invalid or expired reset code.");
        }

        var user = await _userRepository.GetUserWithRolesByEmailAsync(request.Email);

        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            user.Password = _passwordHasher.HashPassword(request.NewPassword);
            user.ModifiedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            resetToken.IsUsed = true;
            resetToken.ModifiedAt = DateTime.UtcNow;
            await _passwordResetTokenRepository.UpdateAsync(resetToken);

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "Password has been reset successfully."
            };
        }
        else
        {
            resetToken.IsUsed = true;
            resetToken.ModifiedAt = DateTime.UtcNow;
            await _passwordResetTokenRepository.UpdateAsync(resetToken);

            var (token, tokenExpires) = _jwtTokenService.GenerateToken(user);
            var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(user);

            var authResponse = new AuthResponse
            {
                Token = token,
                TokenExpires = tokenExpires,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpires = refreshToken.ExpiresAt,
                Email = user.Email,
                UserName = user.UserName,
                UserId = user.Id,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
            };

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "Session continued successfully.",
                AuthResponse = authResponse
            };
        }
    }
}
