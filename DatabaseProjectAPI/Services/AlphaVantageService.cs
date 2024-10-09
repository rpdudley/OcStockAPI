using DatabaseProjectAPI.Entities.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DatabaseProjectAPI.Services;

public interface IAlphaVantageService
{
    Task<decimal> GetStockQuote(string symbol);
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

    public async Task<decimal> GetStockQuote(string symbol)
    {
        try
        {
            string requestUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                // Parse the JSON response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject quoteData = JObject.Parse(jsonResponse);

                // Extract the "05. price" field (latest price)
                string lastPriceStr = quoteData["Global Quote"]?["02. open"]?.ToString();

                if (decimal.TryParse(lastPriceStr, out decimal lastPrice))
                {
                    return lastPrice;
                }
                else
                {
                    throw new Exception("Unable to parse stock quote price.");
                }
            }
            else
            {
                throw new Exception($"Stock quote request unsuccessful. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Stock quote retrieval issue: {ex.Message}");
        }
    }
}

