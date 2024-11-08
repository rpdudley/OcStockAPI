using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using KubsConnect.Settings;
using DatabaseProjectAPI.Services;
using KubsConnect;
using DatabaseProjectAPI.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Load configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

// Configure settings
//builder.Services.Configure<FinnhubSettings>(builder.Configuration.GetSection("Finnhub"));
//builder.Services.Configure<AlphaVantageSettings>(builder.Configuration.GetSection("AlphaVantage"));
//builder.Services.Configure<NewsSettings>(builder.Configuration.GetSection("NewsAPI"));
KubsClient client = new KubsClient(builder.Services, builder.Configuration, builder.Environment);

// Configure DbContext
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DpapiDbContext>(options =>
{
    options.UseMySql(client.config.DBConnectionSettings.RyanWilliamDB, 
        ServerVersion.AutoDetect(client.config.DBConnectionSettings.RyanWilliamDB));
});

// Register services
builder.Services.AddHealthChecks()
    .AddMySql(client.config.DBConnectionSettings.RyanWilliamDB, name: "MySQL Database");

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

builder.Services.AddHostedService<DataCleanupBackgroundService>();
builder.Services.AddHostedService<StockQuoteBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/status");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();