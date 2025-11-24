using WebTemplate.Application.DTOs.Auth;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string? IpAddress = null) : IRequest<AuthResponse>;
