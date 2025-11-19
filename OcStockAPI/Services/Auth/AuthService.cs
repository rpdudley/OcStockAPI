using OcStockAPI.DTOs.Auth;
using OcStockAPI.Services.Email;

namespace OcStockAPI.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
    Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task LogoutAsync();
    Task<AuthResponseDto> PromoteToAdminAsync(string email);
    Task<List<UserDto>> GetAllUsersAsync();
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtService jwtService,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "User with this email already exists"
                };
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                EmailConfirmed = true // For now, auto-confirm emails
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Registration failed: {errors}"
                };
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            // Send welcome email (don't fail registration if email fails)
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Failed to send welcome email to {Email}", user.Email);
            }

            // Generate token
            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtService.GenerateTokenAsync(user, roles);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList()
            };

            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                TokenExpires = DateTime.UtcNow.AddHours(24),
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Email}", registerDto.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Account is deactivated"
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                var message = result.IsLockedOut ? "Account is locked out" :
                             result.IsNotAllowed ? "Login not allowed" :
                             "Invalid email or password";

                return new AuthResponseDto
                {
                    Success = false,
                    Message = message
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate token
            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtService.GenerateTokenAsync(user, roles);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList()
            };

            _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                TokenExpires = DateTime.UtcNow.AddHours(24),
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    public async Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Password change failed: {errors}"
                };
            }

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Password changed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred while changing password"
            };
        }
    }

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist - security best practice
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", forgotPasswordDto.Email);
                return new AuthResponseDto
                {
                    Success = true,
                    Message = "If an account with that email exists, a password reset link has been sent"
                };
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Send password reset email
            try
            {
                var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email, token, user.FullName);
                
                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent successfully to {Email}", forgotPasswordDto.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to {Email} - email service may not be configured", forgotPasswordDto.Email);
                    // Log token for development/debugging (remove in production)
                    _logger.LogInformation("Password reset token for {Email}: {Token}", forgotPasswordDto.Email, token);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Error sending password reset email to {Email}", forgotPasswordDto.Email);
                // Log token as fallback
                _logger.LogInformation("Password reset token for {Email}: {Token}", forgotPasswordDto.Email, token);
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link has been sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", forgotPasswordDto.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred while processing your request"
            };
        }
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid reset request"
                };
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Password reset failed: {errors}"
                };
            }

            _logger.LogInformation("Password reset successfully for user {Email}", resetPasswordDto.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Password reset successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", resetPasswordDto.Email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred while resetting password"
            };
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<AuthResponseDto> PromoteToAdminAsync(string email)
    {
        try
        {
            // SECURITY: Only allow the configured admin email to be promoted
            var allowedAdminEmail = _configuration["AdminUser:Email"];
            if (string.IsNullOrEmpty(allowedAdminEmail))
            {
                _logger.LogError("AdminUser:Email not configured in appsettings");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Admin email not configured"
                };
            }

            if (!email.Equals(allowedAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unauthorized attempt to promote {Email} to Admin. Only {AdminEmail} is allowed.", email, allowedAdminEmail);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Only the system administrator can be promoted to Admin role"
                };
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check if user is already an admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "User is already an admin"
                };
            }

            // Add Admin role
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Failed to promote user: {errors}"
                };
            }

            _logger.LogInformation("User {Email} promoted to Admin", email);

            return new AuthResponseDto
            {
                Success = true,
                Message = $"User {email} has been promoted to Admin"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting user {Email} to Admin", email);
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred while promoting user"
            };
        }
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Roles = roles.ToList()
                });
            }

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return new List<UserDto>();
        }
    }
}