using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Helpers;
using DatabaseProjectAPI.Services;
using KubsConnect;
using KubsConnect.Settings;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

string UserSecretsId = GetUserSecretsId();
KubsClient client;
if (UserSecretsId != null)
{
    builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
    var config = new StartupConfig();
    builder.Configuration.GetSection("StartupConfig").Bind(config);
    client = new KubsClient(builder.Services, config);
}
else
{
    client = new KubsClient(builder.Services, builder.Configuration, builder.Environment);
}

builder.Services.AddDbContext<DpapiDbContext>(options =>
{
    options.UseMySql(client.config.DBConnectionSettings.MySqlDB,
        ServerVersion.AutoDetect(client.config.DBConnectionSettings.MySqlDB));
});

// Register services
builder.Services.AddHealthChecks()
    .AddMySql(client.config.DBConnectionSettings.MySqlDB, name: "MySQL Database");

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

string GetUserSecretsId()
{
    var attribute = typeof(Program).Assembly
        .GetCustomAttribute(typeof(Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute))
        as Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute;

    return attribute?.UserSecretsId;
}