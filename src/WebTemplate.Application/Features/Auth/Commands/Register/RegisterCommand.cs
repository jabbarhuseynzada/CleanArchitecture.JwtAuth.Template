using WebTemplate.Application.DTOs.Auth;
using MediatR;

namespace WebTemplate.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<AuthResponse>;
