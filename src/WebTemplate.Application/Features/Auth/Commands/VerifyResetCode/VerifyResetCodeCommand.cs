using WebTemplate.Application.DTOs.Auth;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Commands.VerifyResetCode;

public record VerifyResetCodeCommand(string Email, string Code, string? NewPassword) : IRequest<ResetPasswordResponse>;
