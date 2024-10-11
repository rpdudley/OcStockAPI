using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Entities.Settings;

namespace DatabaseProjectAPI.Services
{
    public interface IAlphaVantageService
    {
        Task<(decimal Open, decimal Price, long Volume, DateTime LatestTradingDay)> GetStockQuote(string symbol);
    }

    public class AlphaVantageService : IAlphaVantageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AlphaVantageService(HttpClient httpClient, IOptions<AlphaVantageSettings> settings)
        {
            _httpClient = httpClient;
            _apiKey = settings.Value.ApiKey;
        }

        public async Task<(decimal Open, decimal Price, long Volume, DateTime LatestTradingDay)> GetStockQuote(string symbol)//string symbol ONLY VALID STOCK SYMBOLS ARE USED
        {
            string requestUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";
            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();

                var alphaVantageResponse = JsonConvert.DeserializeObject<AlphaVantageResponse>(jsonResponse);
                var quote = alphaVantageResponse?.GlobalQuote;

                if (quote == null)
                {
                    throw new Exception("Failed to retrieve stock quote data.");
                }

                decimal open = quote.Open;
                decimal price = quote.Price;
                long volume = quote.Volume;
                DateTime latestTradingDay = quote.LatestTradingDay;

                return (open, price, volume, latestTradingDay);
            }
            else
            {
                throw new Exception($"Stock quote request unsuccessful. Status code: {response.StatusCode}");
            }
        }
    }
}