using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OcStockAPI.DataContext;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OcStockAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Test endpoints for authentication and authorization")]
public class TestController : ControllerBase
{
    private readonly OcStockDbContext _context;

    public TestController(OcStockDbContext context)
    {
        _context = context;
    }
    [HttpGet("public")]
    [SwaggerOperation(
        Summary = "Public endpoint",
        Description = "A public endpoint that doesn't require authentication"
    )]
    [SwaggerResponse(200, "Public endpoint response")]
    public IActionResult PublicEndpoint()
    {
        return Ok(new { 
            message = "This is a public endpoint, no authentication required",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("protected")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Protected endpoint",
        Description = "A protected endpoint that requires authentication"
    )]
    [SwaggerResponse(200, "Protected endpoint response")]
    [SwaggerResponse(401, "Unauthorized")]
    public IActionResult ProtectedEndpoint()
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var fullName = User.FindFirst("fullName")?.Value;
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new { 
            message = "This is a protected endpoint, authentication required",
            user = new {
                id = userId,
                email = email,
                fullName = fullName,
                roles = roles
            },
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Admin only endpoint",
        Description = "An endpoint that requires Admin role"
    )]
    [SwaggerResponse(200, "Admin endpoint response")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Admin role required")]
    public IActionResult AdminEndpoint()
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new { 
            message = "This is an admin-only endpoint",
            user = new {
                id = userId,
                email = email
            },
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("super-admin")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Super admin test endpoint",
        Description = "Tests super key authentication - accessible only with super key or admin JWT"
    )]
    [SwaggerResponse(200, "Super admin access granted")]
    [SwaggerResponse(401, "Unauthorized")]
    public IActionResult SuperAdminEndpoint()
    {
        var userId = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var fullName = User.FindFirst("fullName")?.Value;
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        var isSuperUser = User.FindFirst("isSuperUser")?.Value == "true";

        return Ok(new { 
            message = isSuperUser ? "?? SUPER KEY ACCESS GRANTED!" : "?? Admin access via JWT",
            authMethod = isSuperUser ? "SuperKey" : "JWT",
            user = new {
                id = userId,
                email = email,
                fullName = fullName,
                roles = roles,
                isSuperUser = isSuperUser
            },
            timestamp = DateTime.UtcNow,
            superKeyInstructions = new {
                header1 = "X-Super-Key: your-super-key-here",
                header2 = "Authorization: SuperKey your-super-key-here",
                yourSuperKey = "Check your user secrets file"
            }
        });
    }

    [HttpGet("database")]
    [SwaggerOperation(
        Summary = "Database connection test",
        Description = "Tests database connectivity and basic operations"
    )]
    [SwaggerResponse(200, "Database test results")]
    [SwaggerResponse(500, "Database connection failed")]
    public async Task<IActionResult> DatabaseTest()
    {
        var testResults = new
        {
            timestamp = DateTime.UtcNow,
            databaseType = _context.Database.IsInMemory() ? "In-Memory" : "PostgreSQL",
            tests = new List<object>()
        };

        try
        {
            // Test 1: Basic connection
            var canConnect = await _context.Database.CanConnectAsync();
            ((List<object>)testResults.tests).Add(new
            {
                test = "Database Connection",
                status = canConnect ? "PASS" : "FAIL",
                message = canConnect ? "Successfully connected to database" : "Failed to connect to database"
            });

            if (!canConnect)
            {
                return StatusCode(500, testResults);
            }

            // Test 2: Check if database exists and can be accessed
            try
            {
                var dbExists = await _context.Database.CanConnectAsync();
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Database Accessibility",
                    status = "PASS",
                    message = "Database is accessible"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Database Accessibility",
                    status = "FAIL",
                    message = $"Database access failed: {ex.Message}"
                });
            }

            // Test 3: Check table existence (try to query a simple table)
            try
            {
                var userCount = await _context.Users.CountAsync();
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Table Access (Users)",
                    status = "PASS",
                    message = $"Successfully queried Users table, found {userCount} users"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Table Access (Users)",
                    status = "FAIL",
                    message = $"Failed to access Users table: {ex.Message}"
                });
            }

            // Test 4: Check other key tables
            try
            {
                var stockCount = await _context.TrackedStocks.CountAsync();
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Table Access (TrackedStocks)",
                    status = "PASS",
                    message = $"Successfully queried TrackedStocks table, found {stockCount} tracked stocks"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Table Access (TrackedStocks)",
                    status = "FAIL",
                    message = $"Failed to access TrackedStocks table: {ex.Message}"
                });
            }

            // Test 5: Test write operation (if not in production)
            try
            {
                // Create a test entry in ApiCallLog
                var testLog = new OcStockAPI.Entities.Settings.ApiCallLog
                {
                    CallType = "Test",
                    CallDate = DateTime.UtcNow,
                    Symbol = "TEST"
                };

                _context.ApiCallLog.Add(testLog);
                await _context.SaveChangesAsync();

                // Clean up the test entry
                _context.ApiCallLog.Remove(testLog);
                await _context.SaveChangesAsync();

                ((List<object>)testResults.tests).Add(new
                {
                    test = "Write Operations",
                    status = "PASS",
                    message = "Successfully performed read/write operations"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Write Operations",
                    status = "FAIL",
                    message = $"Write operation failed: {ex.Message}"
                });
            }

            // Test 6: Connection string info (without exposing sensitive data)
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                const int hostPrefixLength = "Host=".Length; // Length of "Host="
                var hostStart = connectionString?.IndexOf("Host=") ?? -1;
                var hostEnd = connectionString?.IndexOf(";", hostStart) ?? -1;
                var host = hostStart >= 0 && hostEnd > hostStart 
                    ? connectionString.Substring(hostStart + hostPrefixLength, hostEnd - hostStart - hostPrefixLength)
                    : "Unknown";

                ((List<object>)testResults.tests).Add(new
                {
                    test = "Connection Info",
                    status = "INFO",
                    message = _context.Database.IsInMemory() 
                        ? "Using in-memory database" 
                        : $"Connected to PostgreSQL host: {host}"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Connection Info",
                    status = "WARN",
                    message = $"Could not retrieve connection info: {ex.Message}"
                });
            }

            // Determine overall status
            var allTests = (List<object>)testResults.tests;
            var failedTests = allTests.Count(t => ((dynamic)t).status == "FAIL");
            var overallStatus = failedTests == 0 ? "ALL TESTS PASSED" : $"{failedTests} TESTS FAILED";

            return Ok(new
            {
                testResults.timestamp,
                testResults.databaseType,
                overallStatus,
                testResults.tests
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                testResults.timestamp,
                testResults.databaseType,
                overallStatus = "CRITICAL FAILURE",
                error = ex.Message,
                tests = testResults.tests
            });
        }
    }

    [HttpPost("validate-token")]
    [SwaggerOperation(
        Summary = "Validate JWT token",
        Description = "Tests if a JWT token is valid and shows its claims"
    )]
    [SwaggerResponse(200, "Token validation results")]
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { success = false, message = "Token is required" });
            }

            // Remove 'Bearer ' prefix if present
            var token = request.Token.StartsWith("Bearer ") 
                ? request.Token.Substring(7) 
                : request.Token;

            var handler = new JwtSecurityTokenHandler();
            
            // First, check if the token is a valid JWT format
            if (!handler.CanReadToken(token))
            {
                return Ok(new
                {
                    success = false,
                    message = "Invalid JWT token format",
                    timestamp = DateTime.UtcNow
                });
            }

            // Read the token without validation to see its contents
            var jsonToken = handler.ReadJwtToken(token);
            
            // Get JWT settings for validation
            var configuration = HttpContext.RequestServices.GetService<IConfiguration>();
            var jwtSettings = configuration?.GetSection("JwtSettings");

            if (jwtSettings == null)
            {
                return Ok(new
                {
                    success = false,
                    message = "JWT configuration not available",
                    tokenInfo = new
                    {
                        issuer = jsonToken.Issuer,
                        audience = jsonToken.Audiences?.FirstOrDefault(),
                        expires = jsonToken.ValidTo,
                        isExpired = jsonToken.ValidTo < DateTime.UtcNow,
                        claims = jsonToken.Claims.Select(c => new { c.Type, c.Value }).ToList()
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            // Now validate the token properly
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "");
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            
            return Ok(new
            {
                success = true,
                message = "Token is valid",
                tokenInfo = new
                {
                    issuer = jsonToken.Issuer,
                    audience = jsonToken.Audiences?.FirstOrDefault(),
                    expires = jsonToken.ValidTo,
                    isExpired = jsonToken.ValidTo < DateTime.UtcNow,
                    userId = principal.FindFirst("userId")?.Value,
                    email = principal.FindFirst(ClaimTypes.Email)?.Value,
                    roles = principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
                    allClaims = principal.Claims.Select(c => new { c.Type, c.Value }).ToList()
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (SecurityTokenExpiredException)
        {
            return Ok(new
            {
                success = false,
                message = "Token has expired",
                timestamp = DateTime.UtcNow
            });
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Ok(new
            {
                success = false,
                message = "Token has an invalid signature",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                message = $"Token validation failed: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}