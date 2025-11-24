using Asp.Versioning;
using WebTemplate.Application.DTOs.Auth;
using WebTemplate.Application.Features.Auth.Commands.Login;
using WebTemplate.Application.Features.Auth.Commands.Register;
using WebTemplate.Application.Features.Auth.Commands.ForgotPassword;
using WebTemplate.Application.Features.Auth.Commands.RefreshToken;
using WebTemplate.Application.Features.Auth.Commands.ValidateCode;
using WebTemplate.Application.Features.Auth.Commands.VerifyResetCode;
using WebTemplate.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace WebTemplate.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")] // Backwards compatible route
[EnableRateLimiting("per-ip")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(IMediator mediator, IJwtTokenService jwtTokenService)
    {
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
    }

    private string? GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("sliding")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password, GetIpAddress());
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("sliding")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.UserName,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password
        );

        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken, GetIpAddress());
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        await _jwtTokenService.RevokeTokenAsync(request.RefreshToken, GetIpAddress());
        return Ok(new { message = "Token revoked successfully" });
    }

    [HttpPost("revoke-all-tokens")]
    [Authorize]
    public async Task<ActionResult> RevokeAllTokens()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return BadRequest(new { message = "Invalid user" });
        }

        await _jwtTokenService.RevokeAllUserTokensAsync(userGuid, GetIpAddress());
        return Ok(new { message = "All tokens revoked successfully" });
    }

    [HttpGet("test")]
    [Authorize]
    public ActionResult TestAuth()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.Identity?.Name;
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            message = "You are authenticated!",
            userId,
            email = userEmail,
            roles
        });
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public ActionResult AdminOnly()
    {
        return Ok(new { message = "You have admin access!" });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("sliding")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);
        await _mediator.Send(command);
        return Ok(new { message = "If the email exists, a reset code has been sent." });
    }

    [HttpPost("validate-code")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateCode([FromBody] ValidateCodeRequest request)
    {
        var command = new ValidateCodeCommand(request.Email, request.Code);
        var isValid = await _mediator.Send(command);
        return Ok(new { success = isValid, message = "Code is valid." });
    }

    [HttpPost("verify-reset-code")]
    [AllowAnonymous]
    public async Task<ActionResult<ResetPasswordResponse>> VerifyResetCode([FromBody] VerifyResetCodeRequest request)
    {
        var command = new VerifyResetCodeCommand(request.Email, request.Code, request.NewPassword);
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}
