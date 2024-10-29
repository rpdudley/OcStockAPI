using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using DatabaseProjectAPI.Entities;
using KubsConnect.Settings;

namespace DatabaseProjectAPI.Services
{
    public interface IAlphaVantageService
    {
        Task<(string Symbol, decimal Open, decimal Price, long Volume, DateTime LatestTradingDay)> GetStockQuote(string symbol);
    }

    public class AlphaVantageService : IAlphaVantageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<AlphaVantageService> _logger;

        public AlphaVantageService(HttpClient httpClient, AlphaVantageSettings settings, ILogger<AlphaVantageService> logger)
        {
            _httpClient = httpClient;
            _apiKey = settings.ApiKey;
            _logger = logger;
        }

        public async Task<(string Symbol, decimal Open, decimal Price, long Volume, DateTime LatestTradingDay)> GetStockQuote(string symbol)
        {
            string requestUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Log the raw JSON response for debugging purposes
                    _logger.LogInformation("Received response for symbol {Symbol}: {JsonResponse}", symbol, jsonResponse);

                    var alphaVantageResponse = JsonConvert.DeserializeObject<AlphaVantageResponse>(jsonResponse);
                    var quote = alphaVantageResponse?.GlobalQuote;

                    if (quote == null)
                    {
                        throw new Exception("Failed to retrieve stock quote data: Global Quote section is missing.");
                    }

                    // Extract and parse values safely, providing defaults if missing
                    string retrievedSymbol = quote.Symbol ?? symbol;
                    decimal open = quote.Open;
                    decimal price = quote.Price;
                    long volume = quote.Volume;
                    DateTime latestTradingDay = quote.LatestTradingDay;

                    return (retrievedSymbol, open, price, volume, latestTradingDay);
                }
                else
                {
                    throw new Exception($"Stock quote request unsuccessful. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving stock quote data for symbol: {Symbol}", symbol);
                throw new Exception("Failed to retrieve stock quote data.", ex);
            }
        }
    }
}