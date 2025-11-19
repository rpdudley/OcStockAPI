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

#if DEBUG
// TestController is only available in DEBUG builds (development)
// This prevents it from being included in production deployments
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Test endpoints for authentication, authorization, and database testing (DEVELOPMENT ONLY)")]
public class TestController : ControllerBase
{
    private readonly OcStockDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(OcStockDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
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
            databaseType = "PostgreSQL",
            tests = new List<object>()
        };

        try
        {
            // Test 1: Basic connection
            var canConnect = await _context.Database.CanConnectAsync();
            ((List<object>)testResults.tests).Add(new
            {
                test = "Database Connection",
                status = canConnect ? "? PASS" : "? FAIL",
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
                    status = "? PASS",
                    message = "Database is accessible"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Database Accessibility",
                    status = "? FAIL",
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
                    status = "? PASS",
                    message = $"Successfully queried Users table, found {userCount} users"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Table Access (Users)",
                    status = "? FAIL",
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
                    status = "? PASS",
                    message = $"Successfully queried TrackedStocks table, found {stockCount} tracked stocks"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Table Access (TrackedStocks)",
                    status = "? FAIL",
                    message = $"Failed to access TrackedStocks table: {ex.Message}"
                });
            }

            // Test 5: Test write operation
            try
            {
                var testLog = new OcStockAPI.Entities.Settings.ApiCallLog
                {
                    CallType = "Test",
                    CallDate = DateTime.UtcNow,
                    Symbol = "TEST"
                };

                _context.ApiCallLog.Add(testLog);
                await _context.SaveChangesAsync();

                _context.ApiCallLog.Remove(testLog);
                await _context.SaveChangesAsync();

                ((List<object>)testResults.tests).Add(new
                {
                    test = "Write Operations",
                    status = "? PASS",
                    message = "Successfully performed read/write operations"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Write Operations",
                    status = "? FAIL",
                    message = $"Write operation failed: {ex.Message}"
                });
            }

            // Test 6: Connection string info (without exposing sensitive data)
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                var hostStart = connectionString?.IndexOf("Host=") ?? -1;
                var hostEnd = connectionString?.IndexOf(";", hostStart) ?? -1;
                var host = hostStart >= 0 && hostEnd > hostStart 
                    ? connectionString.Substring(hostStart + 5, hostEnd - hostStart - 5)
                    : "Unknown";

                ((List<object>)testResults.tests).Add(new
                {
                    test = "Connection Info",
                    status = "?? INFO",
                    message = $"Connected to PostgreSQL host: {host}"
                });
            }
            catch (Exception ex)
            {
                ((List<object>)testResults.tests).Add(new
                {
                    test = "Connection Info",
                    status = "?? WARN",
                    message = $"Could not retrieve connection info: {ex.Message}"
                });
            }

            var allTests = (List<object>)testResults.tests;
            var failedTests = allTests.Count(t => ((dynamic)t).status.Contains("FAIL"));
            var overallStatus = failedTests == 0 ? "? ALL TESTS PASSED" : $"? {failedTests} TESTS FAILED";

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
            _logger.LogError(ex, "Critical error during database testing");
            return StatusCode(500, new
            {
                testResults.timestamp,
                testResults.databaseType,
                overallStatus = "? CRITICAL FAILURE",
                error = ex.Message,
                stackTrace = ex.StackTrace,
                tests = testResults.tests
            });
        }
    }

    [HttpGet("database/tables")]
    [SwaggerOperation(
        Summary = "List all database tables with row counts",
        Description = "Shows all tables in the database with their record counts"
    )]
    [SwaggerResponse(200, "List of tables with counts")]
    public async Task<IActionResult> GetDatabaseTables()
    {
        try
        {
            var tables = new List<object>
            {
                new { 
                    table = "Users", 
                    count = await _context.Users.CountAsync(),
                    sample = await _context.Users.Take(5).Select(u => new { u.Id, u.Email, u.FirstName, u.LastName }).ToListAsync()
                },
                new { 
                    table = "Roles", 
                    count = await _context.Roles.CountAsync(),
                    sample = await _context.Roles.Take(5).Select(r => new { r.Id, r.Name }).ToListAsync()
                },
                new { 
                    table = "TrackedStocks", 
                    count = await _context.TrackedStocks.CountAsync(),
                    sample = await _context.TrackedStocks.Take(5).Select(ts => new { ts.Id, ts.Symbol, ts.StockName, ts.DateAdded }).ToListAsync()
                },
                new { 
                    table = "Stocks", 
                    count = await _context.Stocks.CountAsync(),
                    sample = await _context.Stocks.Take(5).Select(s => new { s.StockId, s.Symbol, s.Name, s.LastUpdated }).ToListAsync()
                },
                new { 
                    table = "StockHistories", 
                    count = await _context.StockHistories.CountAsync(),
                    sample = await _context.StockHistories.Take(5).Select(sh => new { sh.HistoryId, sh.StockId, sh.Timestamp, sh.ClosedValue }).ToListAsync()
                },
                new { 
                    table = "MarketNews", 
                    count = await _context.MarketNews.CountAsync(),
                    sample = await _context.MarketNews.Take(5).Select(mn => new { mn.NewsId, mn.StockId, mn.Headline, mn.Datetime }).ToListAsync()
                },
                new { 
                    table = "InvestorAccounts", 
                    count = await _context.InvestorAccounts.CountAsync(),
                    sample = await _context.InvestorAccounts.Take(5).Select(ia => new { ia.AccountId, ia.UserId, ia.Name }).ToListAsync()
                },
                new { 
                    table = "ApiCallLog", 
                    count = await _context.ApiCallLog.CountAsync(),
                    sample = await _context.ApiCallLog.Take(5).Select(acl => new { acl.Id, acl.CallType, acl.Symbol, acl.CallDate }).ToListAsync()
                }
            };

            var totalRecords = tables.Sum(t => ((dynamic)t).count);

            return Ok(new
            {
                databaseType = "PostgreSQL",
                connectionString = MaskConnectionString(_context.Database.GetConnectionString() ?? ""),
                totalTables = tables.Count,
                totalRecords,
                timestamp = DateTime.UtcNow,
                tables
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database tables");
            return StatusCode(500, new { error = $"Failed to retrieve tables: {ex.Message}" });
        }
    }

    // ...existing code for other query endpoints...

    [HttpGet("database/summary")]
    [SwaggerOperation(
        Summary = "Database summary statistics",
        Description = "Comprehensive overview of all database tables and their statistics"
    )]
    [SwaggerResponse(200, "Database summary")]
    public async Task<IActionResult> GetDatabaseSummary()
    {
        try
        {
            var summary = new
            {
                databaseType = "PostgreSQL",
                connectionInfo = MaskConnectionString(_context.Database.GetConnectionString() ?? ""),
                timestamp = DateTime.UtcNow,
                
                userStatistics = new
                {
                    totalUsers = await _context.Users.CountAsync(),
                    activeUsers = await _context.Users.Where(u => u.IsActive).CountAsync(),
                    confirmedEmails = await _context.Users.Where(u => u.EmailConfirmed).CountAsync(),
                    lockedOutUsers = await _context.Users.Where(u => u.LockoutEnd > DateTimeOffset.UtcNow).CountAsync(),
                    recentLogins = await _context.Users.Where(u => u.LastLoginAt > DateTime.UtcNow.AddDays(-7)).CountAsync()
                },
                
                stockStatistics = new
                {
                    trackedStocks = await _context.TrackedStocks.CountAsync(),
                    maxTrackedStocks = 20,
                    availableSlots = 20 - await _context.TrackedStocks.CountAsync(),
                    totalStocks = await _context.Stocks.CountAsync(),
                    stockHistoryRecords = await _context.StockHistories.CountAsync(),
                    newsArticles = await _context.MarketNews.CountAsync()
                },
                
                accountStatistics = new
                {
                    investorAccounts = await _context.InvestorAccounts.CountAsync(),
                    portfolios = await _context.Portfolios.CountAsync(),
                    totalRoles = await _context.Roles.CountAsync()
                },
                
                systemStatistics = new
                {
                    apiCallsToday = await _context.ApiCallLog.Where(log => log.CallDate.Date == DateTime.UtcNow.Date).CountAsync(),
                    totalApiCalls = await _context.ApiCallLog.CountAsync(),
                    events = await _context.Events.CountAsync()
                }
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating database summary");
            return StatusCode(500, new { error = $"Failed to generate summary: {ex.Message}" });
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

            var token = request.Token.StartsWith("Bearer ") 
                ? request.Token.Substring(7) 
                : request.Token;

            var handler = new JwtSecurityTokenHandler();
            
            if (!handler.CanReadToken(token))
            {
                return Ok(new
                {
                    success = false,
                    message = "Invalid JWT token format",
                    timestamp = DateTime.UtcNow
                });
            }

            var jsonToken = handler.ReadJwtToken(token);
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

            var secretKey = jwtSettings["SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                return Ok(new
                {
                    success = false,
                    message = "JWT SecretKey not configured - cannot validate tokens",
                    hint = "Set JwtSettings:SecretKey in user secrets or environment variables",
                    tokenInfo = new
                    {
                        issuer = jsonToken.Issuer,
                        audience = jsonToken.Audiences?.FirstOrDefault(),
                        expires = jsonToken.ValidTo,
                        isExpired = jsonToken.ValidTo < DateTime.UtcNow
                    },
                    timestamp = DateTime.UtcNow
                });
            }

            var key = Encoding.UTF8.GetBytes(secretKey);
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
                message = "? Token is valid",
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
                message = "? Token has expired",
                timestamp = DateTime.UtcNow
            });
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Ok(new
            {
                success = false,
                message = "? Token has an invalid signature",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                message = $"? Token validation failed: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Not configured";

        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"Password=[^;]+",
            "Password=***"
        );
    }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}
#else
// TestController is completely removed in RELEASE builds (production)
// This ensures no test endpoints are exposed in production deployments
#endif