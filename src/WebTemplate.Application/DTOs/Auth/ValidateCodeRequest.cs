namespace WebTemplate.Application.DTOs.Auth;

public class ValidateCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
