namespace WebTemplate.Application.DTOs.Auth;

public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthResponse? AuthResponse { get; set; }
}
