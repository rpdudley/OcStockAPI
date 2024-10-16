using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities.Settings;
using DatabaseProjectAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true) 
    .AddEnvironmentVariables();             

builder.Services.Configure<FinnhubSettings>(builder.Configuration.GetSection("Finnhub"));
builder.Services.Configure<AlphaVantageSettings>(builder.Configuration.GetSection("AlphaVantage"));
builder.Services.Configure<NewsSettings>(builder.Configuration.GetSection("NewsAPI"));


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DpapiDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddHttpClient<IFinnhubService, FinnhubService>();
builder.Services.AddHttpClient<IAlphaVantageService, AlphaVantageService>();

builder.Services.AddTransient<IFinnhubService, FinnhubService>();
builder.Services.AddTransient<IAlphaVantageService, AlphaVantageService>();
builder.Services.AddTransient<INewsAPIService, NewsAPIService>();
builder.Services.AddTransient<IInvestorAccountAction, InvestorAccountAction>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();