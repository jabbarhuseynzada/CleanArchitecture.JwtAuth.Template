using WebTemplate.Application.Features.Auth.Commands.ValidateCode;
using WebTemplate.Domain.Interfaces.RepositoryContracts;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Handlers.ValidateCode;

public class ValidateCodeCommandHandler : IRequestHandler<ValidateCodeCommand, bool>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;

    public ValidateCodeCommandHandler(IPasswordResetTokenRepository passwordResetTokenRepository)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
    }

    public async Task<bool> Handle(ValidateCodeCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _passwordResetTokenRepository.GetValidTokenByEmailAndCodeAsync(request.Email, request.Code);

        if (resetToken == null)
        {
            throw new UnauthorizedAccessException("Invalid or expired reset code.");
        }

        return true;
    }
}
