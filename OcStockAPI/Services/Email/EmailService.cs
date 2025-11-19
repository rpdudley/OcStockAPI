using System.Net;
using System.Net.Mail;

namespace OcStockAPI.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string? _smtpServer;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly string? _fromEmail;
    private readonly string? _fromName;
    private readonly bool _isConfigured;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load email configuration from environment variables or appsettings
        _smtpServer = Environment.GetEnvironmentVariable("EmailService__SmtpServer") 
            ?? configuration["EmailService:SmtpServer"];
        
        var portString = Environment.GetEnvironmentVariable("EmailService__SmtpPort") 
            ?? configuration["EmailService:SmtpPort"];
        _smtpPort = int.TryParse(portString, out var port) ? port : 587;
        
        _smtpUsername = Environment.GetEnvironmentVariable("EmailService__SmtpUsername") 
            ?? configuration["EmailService:SmtpUsername"];
        
        _smtpPassword = Environment.GetEnvironmentVariable("EmailService__SmtpPassword") 
            ?? configuration["EmailService:SmtpPassword"];
        
        _fromEmail = Environment.GetEnvironmentVariable("EmailService__FromEmail") 
            ?? configuration["EmailService:FromEmail"];
        
        _fromName = Environment.GetEnvironmentVariable("EmailService__FromName") 
            ?? configuration["EmailService:FromName"] 
            ?? "OcStock API";

        // Check if email service is properly configured
        _isConfigured = !string.IsNullOrEmpty(_smtpServer) 
            && !string.IsNullOrEmpty(_smtpUsername) 
            && !string.IsNullOrEmpty(_smtpPassword) 
            && !string.IsNullOrEmpty(_fromEmail);

        if (!_isConfigured)
        {
            _logger.LogWarning("Email service is not fully configured. Email features will be disabled.");
            _logger.LogWarning("Required: EmailService__SmtpServer, EmailService__SmtpUsername, EmailService__SmtpPassword, EmailService__FromEmail");
        }
        else
        {
            _logger.LogInformation("Email service configured: {SmtpServer}:{Port} from {FromEmail}", 
                _smtpServer, _smtpPort, _fromEmail);
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Cannot send password reset email - email service not configured");
            return false;
        }

        // URL encode the token for safe transmission in URL
        var encodedToken = Uri.EscapeDataString(resetToken);
        var encodedEmail = Uri.EscapeDataString(toEmail);
        
        // Get frontend URL from environment or use default
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
        var resetLink = $"{frontendUrl}/reset-password?token={encodedToken}&email={encodedEmail}";

        var subject = "Password Reset Request - OcStock API";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; 
                   text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .warning {{ color: #ff9800; margin: 15px 0; padding: 10px; background-color: #fff3cd; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>We received a request to reset your password for your OcStock API account.</p>
            <p>Click the button below to reset your password:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; background-color: #f0f0f0; padding: 10px; border-radius: 4px;'>
                {resetLink}
            </p>
            <div class='warning'>
                <strong>?? Security Notice:</strong>
                <ul>
                    <li>This link will expire in 24 hours</li>
                    <li>If you didn't request this reset, please ignore this email</li>
                    <li>Never share this link with anyone</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated email from OcStock API</p>
            <p>© 2024 OcStock API. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        var plainTextBody = $@"
Password Reset Request - OcStock API

Hello {userName},

We received a request to reset your password for your OcStock API account.

Click this link to reset your password:
{resetLink}

SECURITY NOTICE:
- This link will expire in 24 hours
- If you didn't request this reset, please ignore this email
- Never share this link with anyone

This is an automated email from OcStock API
© 2024 OcStock API. All rights reserved.
";

        return await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Cannot send welcome email - email service not configured");
            return false;
        }

        var subject = "Welcome to OcStock API! ??";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .feature {{ padding: 10px; margin: 10px 0; background-color: white; border-radius: 4px; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Welcome to OcStock API!</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>Thank you for registering with OcStock API! Your account has been successfully created.</p>
            
            <h3>What you can do:</h3>
            <div class='feature'>?? <strong>Track Stocks:</strong> Monitor up to 20 stocks simultaneously</div>
            <div class='feature'>?? <strong>Market News:</strong> Get latest news for your tracked stocks</div>
            <div class='feature'>?? <strong>Economic Data:</strong> Access CPI, unemployment, and interest rates</div>
            <div class='feature'>?? <strong>Stock History:</strong> View historical performance data</div>
            
            <p>Start exploring by logging into your account and adding stocks to track!</p>
        </div>
        <div class='footer'>
            <p>This is an automated email from OcStock API</p>
            <p>© 2024 OcStock API. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        var plainTextBody = $@"
Welcome to OcStock API!

Hello {userName},

Thank you for registering with OcStock API! Your account has been successfully created.

What you can do:
- Track Stocks: Monitor up to 20 stocks simultaneously
- Market News: Get latest news for your tracked stocks
- Economic Data: Access CPI, unemployment, and interest rates
- Stock History: View historical performance data

Start exploring by logging into your account and adding stocks to track!

This is an automated email from OcStock API
© 2024 OcStock API. All rights reserved.
";

        return await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Cannot send email - email service not configured");
            return false;
        }

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_fromEmail!, _fromName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = plainTextBody;
            message.IsBodyHtml = false;

            // Add HTML alternative view
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
            message.AlternateViews.Add(htmlView);

            using var smtpClient = new SmtpClient(_smtpServer, _smtpPort);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            await smtpClient.SendMailAsync(message);
            
            _logger.LogInformation("Email sent successfully to {ToEmail} with subject: {Subject}", toEmail, subject);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {ToEmail}: {Error}", toEmail, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            return false;
        }
    }
}
