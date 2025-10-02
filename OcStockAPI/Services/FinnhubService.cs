using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using OcStockAPI.Settings;
using Microsoft.Extensions.Options;
using OcStockAPI.Entities;

namespace OcStockAPI.Services
{
    public interface IFinnhubService
    {
        Task<JsonObject> GetStockDataAsync(string symbol, DateTime fromDate, DateTime toDate);
        Task<FinnhubMarketStatus> MarkStatusAsync();
    }

    public class FinnhubService : IFinnhubService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public FinnhubService(HttpClient httpClient, IOptions<AppSettings> settings)
        {
            _httpClient = httpClient;
            _apiKey = Environment.GetEnvironmentVariable("FINNHUB__APIKEY") ?? settings.Value.Finnhub.ApiKey;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<JsonObject> GetStockDataAsync(string symbol, DateTime fromDate, DateTime toDate)
        {
            long fromTimestamp = new DateTimeOffset(fromDate).ToUnixTimeSeconds();
            long toTimestamp = new DateTimeOffset(toDate).ToUnixTimeSeconds();
            string url = $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution=D&from={fromTimestamp}&to={toTimestamp}&token={_apiKey}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResult = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonObject>(jsonResult, _jsonOptions)!;
        }
        
        public async Task<FinnhubMarketStatus> MarkStatusAsync()
        {
            string url = $"https://finnhub.io/api/v1/stock/market-status?exchange=US&token={_apiKey}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            string jsonResult = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FinnhubMarketStatus>(jsonResult, _jsonOptions)!;
        }
    }
}
