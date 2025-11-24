using WebTemplate.Application.Features.Auth.Commands.ForgotPassword;
using WebTemplate.Domain.Entities;
using WebTemplate.Domain.Interfaces;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Handlers.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailService = emailService;
    }

    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);

        if (user == null)
        {
            return true;
        }

        await _passwordResetTokenRepository.InvalidateUserTokensAsync(user.Id);

        var code = GenerateResetCode();

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IsDeleted = false
        };

        await _passwordResetTokenRepository.AddAsync(resetToken);

        await _emailService.SendPasswordResetCodeAsync(user.Email, code, user.UserName);

        return true;
    }

    private static string GenerateResetCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
