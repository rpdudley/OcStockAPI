using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OcStockAPI.Middleware;

public class SuperKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string SuperKey { get; set; } = string.Empty;
}

public class SuperKeyAuthenticationHandler : AuthenticationHandler<SuperKeyAuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public SuperKeyAuthenticationHandler(
        IOptionsMonitor<SuperKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var superKey = _configuration["SuperKey"];
        
        // Skip if no super key is configured
        if (string.IsNullOrEmpty(superKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Check for X-Super-Key header
        if (Request.Headers.TryGetValue("X-Super-Key", out var headerValue))
        {
            var providedKey = headerValue.ToString();
            
            if (providedKey == superKey)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "SuperUser"),
                    new Claim(ClaimTypes.Name, "Super Admin"),
                    new Claim(ClaimTypes.Email, "superadmin@ocstock.dev"),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("userId", "0"),
                    new Claim("fullName", "Super Admin"),
                    new Claim("isSuperUser", "true")
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                Logger.LogInformation("Super key authentication successful");
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        // Check Authorization header for super key (alternative format)
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.ToString();
            if (authValue.StartsWith("SuperKey ", StringComparison.OrdinalIgnoreCase))
            {
                var providedKey = authValue.Substring("SuperKey ".Length);
                
                if (providedKey == superKey)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "SuperUser"),
                        new Claim(ClaimTypes.Name, "Super Admin"),
                        new Claim(ClaimTypes.Email, "superadmin@ocstock.dev"),
                        new Claim(ClaimTypes.Role, "Admin"),
                        new Claim(ClaimTypes.Role, "User"),
                        new Claim("userId", "0"),
                        new Claim("fullName", "Super Admin"),
                        new Claim("isSuperUser", "true")
                    };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    Logger.LogInformation("Super key authentication successful via Authorization header");
                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
            }
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}