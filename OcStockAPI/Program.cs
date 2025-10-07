using OcStockAPI.Actions;
using OcStockAPI.DataContext;
using OcStockAPI.Settings;
using OcStockAPI.Services;
using OcStockAPI.Helpers;

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

// Handle database connection string
string connectionString;
try
{
    // Try to get the connection string from the standard ConnectionStrings section
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        // Fallback to AppSettings components if available
        connectionString = appSettings.Database.GetEffectiveConnectionString();
    }
}
catch (InvalidOperationException)
{
    // Fallback to environment variable if components are not available
    var envConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrEmpty(envConnectionString))
    {
        throw new InvalidOperationException(
            "Database connection string is required. Provide either:\n" +
            "1. Full connection string in ConnectionStrings:DefaultConnection\n" +
            "2. Individual components (Database:Host, Database:Database, Database:Username, Database:Password)\n" +
            "3. DATABASE_URL environment variable");
    }
    
    // Handle PostgreSQL URL format (like Supabase provides)
    if (envConnectionString.StartsWith("postgresql://"))
    {
        var dbSettings = DatabaseConnectionSettings.FromPostgreSqlUrl(envConnectionString);
        connectionString = dbSettings.ToConnectionString();
    }
    else
    {
        connectionString = envConnectionString;
    }
}

// Update the app settings with the final connection string
appSettings.Database.ConnectionString = connectionString;

// Register settings
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton(appSettings);

// Configure DbContext for PostgreSQL
builder.Services.AddDbContext<OcStockDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Register services
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL Database");

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

builder.Services.AddHostedService<DataCleanupBackgroundService>();
builder.Services.AddHostedService<StockQuoteBackgroundService>();
builder.Services.AddHostedService<NewsBackgroundService>();
builder.Services.AddHostedService<EventsBackgroundService>();

// Add CORS for your frontend on Render
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000",
                "https://*.onrender.com" // Allow any Render subdomain
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
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
    
    // Add security definitions if you plan to add authentication later
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Always enable Swagger for easy API testing (both Development and Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OC Stock API v1");
    c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger instead of root
    c.DocumentTitle = "OC Stock API Documentation";
    c.DisplayRequestDuration();
    
    // Additional UI improvements
    c.DefaultModelsExpandDepth(-1); // Hide models section by default
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse all operations by default
});

app.UseCors("AllowFrontend");
app.MapHealthChecks("/health"); // Standard health check endpoint

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
