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
    Console.WriteLine("?? Using PostgreSQL database");
    
    // Configure DbContext for PostgreSQL
    builder.Services.AddDbContext<OcStockDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
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

// Register settings
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton(appSettings);

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

// Temporarily comment out background services to allow testing of the main functionality
// builder.Services.AddHostedService<DataCleanupBackgroundService>();
// builder.Services.AddHostedService<StockQuoteBackgroundService>();
// builder.Services.AddHostedService<NewsBackgroundService>();
// builder.Services.AddHostedService<EventsBackgroundService>();

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

// Initialize database (create tables for in-memory database)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OcStockDbContext>();
    
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"?? Database initialization warning: {ex.Message}");
    }
}

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
app.UseStaticFiles(); // Enable static file serving
app.MapHealthChecks("/health"); // Standard health check endpoint

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
