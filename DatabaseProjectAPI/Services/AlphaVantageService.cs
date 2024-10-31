using Newtonsoft.Json;
using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Helpers;
using KubsConnect.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatabaseProjectAPI.Services
{
    public interface IAlphaVantageService
    {
        Task<StockQuote> GetStockQuoteAsync(string symbol);
    }

    public class AlphaVantageService : IAlphaVantageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<AlphaVantageService> _logger;

        public AlphaVantageService(HttpClient httpClient, IOptions<AlphaVantageSettings> options, ILogger<AlphaVantageService> logger)
        {
            _httpClient = httpClient;
            _apiKey = options.Value.ApiKey;
            _logger = logger;
        }

        public async Task<StockQuote> GetStockQuoteAsync(string symbol)
        {
            string requestUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Received response for symbol {Symbol}: {JsonResponse}", symbol, jsonResponse);

                    // Deserialize error responses
                    var errorResponse = JsonConvert.DeserializeObject<AlphaVantageErrorResponse>(jsonResponse);
                    if (errorResponse?.Note != null)
                    {
                        _logger.LogWarning("API rate limit exceeded: {Note}", errorResponse.Note);
                        throw new ApiRateLimitExceededException("API rate limit exceeded. Please try again later.");
                    }

                    var errorMessage = JsonConvert.DeserializeObject<AlphaVantageErrorMessage>(jsonResponse);
                    if (errorMessage?.ErrorMessage != null)
                    {
                        _logger.LogError("Error from API: {ErrorMessage}", errorMessage.ErrorMessage);
                        throw new InvalidApiResponseException($"API Error: {errorMessage.ErrorMessage}");
                    }

                    // Deserialize the main response
                    var alphaVantageResponse = JsonConvert.DeserializeObject<AlphaVantageResponse>(jsonResponse);

                    var quote = alphaVantageResponse?.GlobalQuote;

                    if (quote == null)
                    {
                        throw new InvalidApiResponseException("Failed to retrieve stock quote data: Global Quote section is missing.");
                    }

                    // Validate data
                    if (string.IsNullOrEmpty(quote.Symbol) ||
                        quote.Open == 0 ||
                        quote.Price == 0 ||
                        quote.Volume == 0 ||
                        quote.LatestTradingDay == default)
                    {
                        throw new InvalidApiResponseException("Incomplete data received from the API.");
                    }

                    var stockQuote = new StockQuote
                    {
                        Symbol = quote.Symbol,
                        Open = quote.Open,
                        Price = quote.Price,
                        Volume = quote.Volume,
                        LatestTradingDay = quote.LatestTradingDay
                    };

                    return stockQuote;
                }
                else
                {
                    _logger.LogError("Stock quote request unsuccessful. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Stock quote request unsuccessful. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while retrieving stock quote data for symbol: {Symbol}", symbol);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing data for symbol: {Symbol}", symbol);
                throw new InvalidApiResponseException("JSON parsing error occurred.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving stock quote data for symbol: {Symbol}", symbol);
                throw;
            }
        }
        public class ApiRateLimitExceededException : Exception
        {
            public ApiRateLimitExceededException(string message) : base(message) { }
        }

        public class InvalidApiResponseException : Exception
        {
            public InvalidApiResponseException(string message) : base(message) { }

            public InvalidApiResponseException(string message, Exception innerException) : base(message, innerException) { }
        }

    }
}
