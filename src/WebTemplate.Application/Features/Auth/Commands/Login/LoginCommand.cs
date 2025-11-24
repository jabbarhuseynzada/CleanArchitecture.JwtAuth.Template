using WebTemplate.Application.DTOs.Auth;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password, string? IpAddress = null) : IRequest<AuthResponse>;
