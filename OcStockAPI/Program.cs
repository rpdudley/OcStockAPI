using OcStockAPI.Actions;
using OcStockAPI.DataContext;
using OcStockAPI.Settings;
using OcStockAPI.Services;
using OcStockAPI.Services.Auth;
using OcStockAPI.Helpers;
using OcStockAPI.Entities.Identity;
using OcStockAPI.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration sources - Render will provide environment variables
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

// Configure settings from environment variables (Render style)
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Handle database connection string
string connectionString = "";
bool useInMemoryDatabase = false;

try
{
    // Try to get the connection string from the standard ConnectionStrings section
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    
    if (string.IsNullOrEmpty(connectionString))
    {
        // Fallback to AppSettings components if available
        connectionString = appSettings.Database.GetEffectiveConnectionString();
    }
    
    Console.WriteLine($"Attempting to use connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
    
    // Test the connection first
    using (var testConnection = new Npgsql.NpgsqlConnection(connectionString))
    {
        testConnection.Open();
        Console.WriteLine("? Successfully connected to PostgreSQL database");
        testConnection.Close();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"? Failed to connect to PostgreSQL database: {ex.Message}");
    Console.WriteLine("?? Switching to in-memory database for testing...");
    useInMemoryDatabase = true;
}

// Configure DbContext based on connection test
if (useInMemoryDatabase)
{
    Console.WriteLine("?? Using IN-MEMORY database - Data will NOT persist between restarts!");
    
    builder.Services.AddDbContext<OcStockDbContext>(options =>
    {
        options.UseInMemoryDatabase("OcStockTestDb");
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    });
    
    // Add basic health check
    builder.Services.AddHealthChecks();
}
else
{
    Console.WriteLine("? Using PostgreSQL database");
    
    // Configure DbContext for PostgreSQL
    builder.Services.AddDbContext<OcStockDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        // SECURITY FIX: Only enable sensitive data logging in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
        }
    });
    
    // Register health check with database
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "PostgreSQL Database");
}

// Update the app settings with the final connection string
if (!useInMemoryDatabase)
{
    appSettings.Database.ConnectionString = connectionString;
}

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
    options.Lockout.MaxFailedAccessAttempts = 3; // Set to 3 for enhanced security
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    // TODO: Enable email confirmation in production when email service is implemented
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production with email service
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<OcStockDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // FIXED: Require HTTPS in production
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

// SECURITY FIX: Only enable SuperKey in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication()
        .AddScheme<SuperKeyAuthenticationSchemeOptions, SuperKeyAuthenticationHandler>("SuperKey", options => 
        {
            options.SuperKey = builder.Configuration["SuperKey"] ?? "";
        });
    
    Console.WriteLine("?? WARNING: SuperKey authentication enabled (DEVELOPMENT ONLY)");
}

// Configure authentication policies
builder.Services.AddAuthorization(options =>
{
    var schemes = new List<string> { JwtBearerDefaults.AuthenticationScheme };
    
    // Only add SuperKey in development
    if (builder.Environment.IsDevelopment())
    {
        schemes.Add("SuperKey");
    }
    
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(schemes.ToArray())
        .Build();
});

// Register settings
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton(appSettings);

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

// Background services - ONLY in production to avoid rate limiting during development
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
    Console.WriteLine("?? Background services disabled (DEVELOPMENT - prevents API rate limiting)");
}

// Add CORS for your frontend on Render
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = new List<string>();
        
        // Add development origin
        if (builder.Environment.IsDevelopment())
        {
            allowedOrigins.Add("http://localhost:3000");
            allowedOrigins.Add("http://localhost:5173"); // Vite default
        }
        
        // Add production frontend URL from environment variable
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrEmpty(frontendUrl))
        {
            allowedOrigins.Add(frontendUrl);
        }
        
        // SECURITY WARNING: Only use wildcard in development or for specific trusted domains
        // In production, specify exact URLs
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
            // Production: Strict CORS - only allow specific origins
            if (allowedOrigins.Any())
            {
                policy.WithOrigins(allowedOrigins.ToArray())
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                Console.WriteLine("?? WARNING: No CORS origins configured for production!");
            }
        }
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure System.Text.Json for better performance
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
    
    // Enable annotations for better Swagger documentation
    c.EnableAnnotations();
    
    // Add JWT security definition
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
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<OcStockDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    
    try
    {
        // For in-memory database, ensure it's created
        if (useInMemoryDatabase)
        {
            await dbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("? In-memory database tables created successfully");
        }
        else
        {
            // For PostgreSQL, test the connection
            var canConnect = await dbContext.Database.CanConnectAsync();
            Console.WriteLine(canConnect ? "? PostgreSQL database connection verified" : "? PostgreSQL database connection failed");
        }

        // Seed default roles
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
                Console.WriteLine($"? Created role: {roleName}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"?? Database initialization warning: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
// SECURITY FIX: Only enable Swagger in Development
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
}
else
{
    // In production, you might want to keep Swagger but protect it with authentication
    // Or completely disable it for security
    Console.WriteLine("?? Swagger UI disabled in production environment");
}

app.UseCors("AllowFrontend");
app.UseStaticFiles(); // Enable static file serving

// SECURITY FIX: Add rate limiting
app.UseIpRateLimiting();

app.MapHealthChecks("/health"); // Standard health check endpoint

app.UseHttpsRedirection();

// Authentication & Authorization middleware order is important
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
