namespace OcStockAPI.Services.Email;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName);
    Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody);
}
