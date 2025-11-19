using OcStockAPI.Actions;
using OcStockAPI.DataContext;
using OcStockAPI.Settings;
using OcStockAPI.Services;
using OcStockAPI.Services.Auth;
using OcStockAPI.Services.Email;
using OcStockAPI.Helpers;
using OcStockAPI.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

// Configure settings from environment variables
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ===================================================================
// DATABASE CONFIGURATION - POSTGRESQL ONLY (NO IN-MEMORY FALLBACK)
// ===================================================================

string connectionString = "";

// Try to get the connection string
connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";

if (string.IsNullOrEmpty(connectionString))
{
    // Fallback to AppSettings components if available
    try
    {
        connectionString = appSettings.Database.GetEffectiveConnectionString();
    }
    catch (Exception ex)
    {
        Console.WriteLine("? CRITICAL ERROR: No database connection string found!");
        Console.WriteLine($"   Error: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("?? Connection string must be configured in one of:");
        Console.WriteLine("   1. User Secrets: ConnectionStrings:DefaultConnection");
        Console.WriteLine("   2. Environment Variable: ConnectionStrings__DefaultConnection");
        Console.WriteLine("   3. appsettings.json: ConnectionStrings:DefaultConnection");
        Console.WriteLine();
        Console.WriteLine("?? To set via user secrets:");
        Console.WriteLine("   dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"your-connection-string\"");
        Console.WriteLine();
        throw new InvalidOperationException("Database connection string is not configured. Application cannot start without PostgreSQL connection.");
    }
}

// Mask password for console output
string MaskConnectionString(string connString)
{
    return System.Text.RegularExpressions.Regex.Replace(
        connString,
        @"Password=[^;]+",
        "Password=***"
    );
}

Console.WriteLine("?? Connecting to PostgreSQL database...");
Console.WriteLine($"   Connection: {MaskConnectionString(connectionString)}");

// Test the connection - FAIL IMMEDIATELY if it doesn't work
try
{
    using (var testConnection = new Npgsql.NpgsqlConnection(connectionString))
    {
        testConnection.Open();
        var serverVersion = testConnection.ServerVersion;
        Console.WriteLine($"? Successfully connected to PostgreSQL!");
        Console.WriteLine($"   Server Version: {serverVersion}");
        testConnection.Close();
    }
}
catch (Npgsql.NpgsqlException npgEx)
{
    Console.WriteLine();
    Console.WriteLine("? CRITICAL ERROR: Failed to connect to PostgreSQL database!");
    Console.WriteLine($"   Error: {npgEx.Message}");
    Console.WriteLine();
    Console.WriteLine("?? Common causes:");
    Console.WriteLine("   1. PostgreSQL server is not running");
    Console.WriteLine("   2. Wrong host/port in connection string");
    Console.WriteLine("   3. Invalid username or password");
    Console.WriteLine("   4. Database does not exist");
    Console.WriteLine("   5. Firewall blocking connection");
    Console.WriteLine("   6. SSL/TLS configuration mismatch");
    Console.WriteLine();
    Console.WriteLine($"?? Your connection string (masked): {MaskConnectionString(connectionString)}");
    Console.WriteLine();
    Console.WriteLine("?? Verify your PostgreSQL server is accessible:");
    Console.WriteLine("   - Check if PostgreSQL service is running");
    Console.WriteLine("   - Test connection with a PostgreSQL client (pgAdmin, psql, etc.)");
    Console.WriteLine("   - Verify firewall settings");
    Console.WriteLine();
    throw new InvalidOperationException("Cannot connect to PostgreSQL database. Application cannot start without a valid database connection.", npgEx);
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("? CRITICAL ERROR: Unexpected error while connecting to database!");
    Console.WriteLine($"   Error Type: {ex.GetType().Name}");
    Console.WriteLine($"   Error: {ex.Message}");
    Console.WriteLine();
    throw new InvalidOperationException("Database connection failed. Application cannot start.", ex);
}

// Configure DbContext - POSTGRESQL ONLY
Console.WriteLine("??  Configuring Entity Framework with PostgreSQL...");

builder.Services.AddDbContext<OcStockDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    
    // Enable sensitive data logging in development only
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    
    // Add query logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.LogTo(Console.WriteLine, new[] { 
            Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting 
        });
    }
});

// Register health check with database
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL Database", tags: new[] { "database", "postgresql" });

Console.WriteLine("? Entity Framework configured with PostgreSQL");

// Update the app settings with the connection string
appSettings.Database.ConnectionString = connectionString;

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings - SECURITY: 3 failed attempts = 5 minute lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<OcStockDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "";

// Only require JWT configuration in production
if (!builder.Environment.IsDevelopment() && string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured for production environment");
}

// Configure authentication - in development, JWT is optional
if (!string.IsNullOrEmpty(secretKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
    
    Console.WriteLine("? JWT Authentication configured");
}
else
{
    builder.Services.AddAuthentication();
    Console.WriteLine("??  Running in DEVELOPMENT mode without JWT - Authentication disabled");
}

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.FallbackPolicy = null;
        Console.WriteLine("??  WARNING: Anonymous access enabled (DEVELOPMENT ONLY)");
    }
    else
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .Build();
    }
});

// Register settings
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton(appSettings);

// Register email service
builder.Services.AddTransient<IEmailService, EmailService>();

// Register authentication services
builder.Services.AddTransient<IJwtService, JwtService>();
builder.Services.AddTransient<IAuthService, AuthService>();

// Register services
builder.Services.AddHttpClient<IFinnhubService, FinnhubService>();
builder.Services.AddHttpClient<IAlphaVantageService, AlphaVantageService>();

builder.Services.AddTransient<IFinnhubService, FinnhubService>();
builder.Services.AddTransient<IAlphaVantageService, AlphaVantageService>();
builder.Services.AddTransient<INewsAPIService, NewsAPIService>();
builder.Services.AddTransient<IInvestorAccountAction, InvestorAccountAction>();
builder.Services.AddTransient<ITrackedStockAction, TrackedStockAction>();
builder.Services.AddTransient<IStockHistoryAction, StockHistoryAction>();
builder.Services.AddTransient<IApiRequestLogger, ApiRequestLogger>();
builder.Services.AddTransient<IAutoDeleteService, AutoDeleteAction>();
builder.Services.AddTransient<IStockAction, StockAction>();
builder.Services.AddTransient<IMarketNewsAction, MarketNewsAction>();
builder.Services.AddTransient<IEventsAction, EventsAction>();
builder.Services.AddTransient<ITrackedStockService, TrackedStockService>();

// Background services - ONLY in production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DataCleanupBackgroundService>();
    builder.Services.AddHostedService<StockQuoteBackgroundService>();
    builder.Services.AddHostedService<NewsBackgroundService>();
    builder.Services.AddHostedService<EventsBackgroundService>();
    Console.WriteLine("? Background services enabled (PRODUCTION)");
}
else
{
    Console.WriteLine("??  Background services disabled (DEVELOPMENT - prevents API rate limiting)");
}

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = new List<string>();
        
        if (builder.Environment.IsDevelopment())
        {
            allowedOrigins.Add("http://localhost:3000");
            allowedOrigins.Add("http://localhost:5173");
        }
        
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrEmpty(frontendUrl))
        {
            allowedOrigins.Add(frontendUrl);
        }
        
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowedToAllowWildcardSubdomains();
            policy.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            if (allowedOrigins.Any())
            {
                policy.WithOrigins(allowedOrigins.ToArray())
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                Console.WriteLine("??  WARNING: No CORS origins configured for production!");
            }
        }
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "OC Stock API", 
        Version = "v1",
        Description = "Modern stock market data API built with .NET 8 providing comprehensive financial data including stock quotes, market news, portfolio management, and economic indicators."
    });
    
    c.EnableAnnotations();
    
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Initialize database and seed roles
Console.WriteLine();
Console.WriteLine("???  Initializing database...");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<OcStockDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    
    try
    {
        // Verify database connection
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            Console.WriteLine("? CRITICAL: Cannot connect to database during initialization!");
            throw new InvalidOperationException("Database connection lost");
        }
        
        Console.WriteLine("? Database connection verified");
        
        // Apply any pending migrations automatically
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            Console.WriteLine($"?? Applying {pendingMigrations.Count()} pending migrations...");
            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($"   - {migration}");
            }
            
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("? Database migrations applied successfully");
        }
        else
        {
            Console.WriteLine("? Database is up to date (no pending migrations)");
        }

        // Seed default roles
        Console.WriteLine("?? Seeding default roles...");
        var roles = new[] { "Admin", "User" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole
                {
                    Name = roleName,
                    Description = $"{roleName} role"
                };
                await roleManager.CreateAsync(role);
                Console.WriteLine($"   ? Created role: {roleName}");
            }
            else
            {
                Console.WriteLine($"   ??  Role already exists: {roleName}");
            }
        }
        
        Console.WriteLine("? Database initialization complete!");
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("? CRITICAL ERROR during database initialization!");
        Console.WriteLine($"   Error: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("?? Possible issues:");
        Console.WriteLine("   1. Database connection was lost");
        Console.WriteLine("   2. Migration failed (check migration files)");
        Console.WriteLine("   3. Insufficient database permissions");
        Console.WriteLine("   4. Database schema conflicts");
        Console.WriteLine();
        throw;
    }
}

Console.WriteLine();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OC Stock API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "OC Stock API Documentation";
        c.DisplayRequestDuration();
        c.DefaultModelsExpandDepth(-1);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
    
    Console.WriteLine("?? Swagger UI enabled at: /swagger");
}
else
{
    Console.WriteLine("??  Swagger UI disabled in production environment");
}

app.UseCors("AllowFrontend");
app.UseIpRateLimiting();
app.MapHealthChecks("/health");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine();
Console.WriteLine("?? Application starting...");
Console.WriteLine("?? Database: PostgreSQL (IN-MEMORY DISABLED)");
Console.WriteLine("?? All data persists to real database");
Console.WriteLine();

app.Run();
