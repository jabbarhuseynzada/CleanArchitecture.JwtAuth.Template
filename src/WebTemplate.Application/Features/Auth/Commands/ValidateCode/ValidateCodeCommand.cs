using MediatR;

namespace WebTemplate.Application.Features.Auth.Commands.ValidateCode;

public record ValidateCodeCommand(string Email, string Code) : IRequest<bool>;
