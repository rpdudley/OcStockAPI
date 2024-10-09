using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DatabaseProjectAPI.Services;
using DatabaseProjectAPI.Entities.Settings;

var builder = WebApplication.CreateBuilder(args);

// Load configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true) // For development only
    .AddEnvironmentVariables();             

builder.Services.Configure<FinnhubSettings>(builder.Configuration.GetSection("Finnhub"));
builder.Services.Configure<AlphaVantageSettings>(builder.Configuration.GetSection("AlphaVantage"));

//builder.Services.AddSingleton<(builder.Services.GetRe)>

builder.Services.AddHttpClient<IFinnhubService, FinnhubService>();
builder.Services.AddHttpClient<IAlphaVantageService, AlphaVantageService>();

builder.Services.AddTransient<IFinnhubService, FinnhubService>();
builder.Services.AddTransient<IAlphaVantageService, AlphaVantageService>();

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