using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using OcStockAPI.DTOs.Auth;
using OcStockAPI.Services.Auth;

namespace OcStockAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Authentication and user management endpoints")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register a new user",
        Description = "Creates a new user account with the provided information"
    )]
    [SwaggerResponse(200, "Registration successful", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Registration failed", typeof(AuthResponseDto))]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = $"Validation failed: {string.Join(", ", errors)}"
            });
        }

        var result = await _authService.RegisterAsync(registerDto);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Authenticate user",
        Description = "Authenticates a user and returns a JWT token"
    )]
    [SwaggerResponse(200, "Login successful", typeof(AuthResponseDto))]
    [SwaggerResponse(401, "Login failed", typeof(AuthResponseDto))]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = $"Validation failed: {string.Join(", ", errors)}"
            });
        }

        var result = await _authService.LoginAsync(loginDto);
        
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Change user password",
        Description = "Changes the password for the authenticated user"
    )]
    [SwaggerResponse(200, "Password changed successfully", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Password change failed", typeof(AuthResponseDto))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<AuthResponseDto>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = $"Validation failed: {string.Join(", ", errors)}"
            });
        }

        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new AuthResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [SwaggerOperation(
        Summary = "Request password reset",
        Description = "Sends a password reset email to the user"
    )]
    [SwaggerResponse(200, "Password reset request processed", typeof(AuthResponseDto))]
    public async Task<ActionResult<AuthResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = $"Validation failed: {string.Join(", ", errors)}"
            });
        }

        var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [SwaggerOperation(
        Summary = "Reset password",
        Description = "Resets the user's password using a reset token"
    )]
    [SwaggerResponse(200, "Password reset successful", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Password reset failed", typeof(AuthResponseDto))]
    public async Task<ActionResult<AuthResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = $"Validation failed: {string.Join(", ", errors)}"
            });
        }

        var result = await _authService.ResetPasswordAsync(resetPasswordDto);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get current user info",
        Description = "Returns information about the authenticated user"
    )]
    [SwaggerResponse(200, "User information", typeof(UserDto))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "User not found")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Logout user",
        Description = "Logs out the authenticated user"
    )]
    [SwaggerResponse(200, "Logout successful")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok(new { Success = true, Message = "Logout successful" });
    }

    [HttpPost("promote-to-admin")]
    [Authorize(Roles = "Admin")] // SECURITY FIX: Require admin role
    [SwaggerOperation(
        Summary = "Promote user to Admin (Restricted)",
        Description = "Promotes a user to Admin role. Only the configured admin email can be promoted. Requires existing Admin authentication."
    )]
    [SwaggerResponse(200, "User promoted successfully", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Promotion failed", typeof(AuthResponseDto))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Admin role required or email not authorized")]
    public async Task<ActionResult<AuthResponseDto>> PromoteToAdmin([FromBody] PromoteToAdminDto dto)
    {
        var result = await _authService.PromoteToAdminAsync(dto.Email);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")] // SECURITY FIX: Require admin role
    [SwaggerOperation(
        Summary = "Get all users",
        Description = "Returns a list of all users. Requires Admin authentication."
    )]
    [SwaggerResponse(200, "List of users", typeof(List<UserDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Admin role required")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }
}