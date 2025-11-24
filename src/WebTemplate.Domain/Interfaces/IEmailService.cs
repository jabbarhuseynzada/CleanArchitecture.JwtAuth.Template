namespace WebTemplate.Domain.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string toEmail, string code, string userName);
}
