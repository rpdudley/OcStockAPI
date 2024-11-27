using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Helpers;
using KubsConnect.Settings;
using Newtonsoft.Json;

namespace DatabaseProjectAPI.Services
{
    public interface IAlphaVantageService
    {
        Task<StockQuote> GetStockQuoteAsync(string symbol);
        Task<CPIdata> GetCPIdataAsync();
        Task<FederalInterestRate> GetFederalInterestRateAsync();
        Task<UnemploymentRate> GetUnemploymentRateAsync();
        Task<Inflation> GetInflationAsync();
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

        public async Task<CPIdata> GetCPIdataAsync()
        {
            string requestUrl = $"https://www.alphavantage.co/query?function=CPI&interval=monthly&apikey={_apiKey}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Received CPI response: {JsonResponse}", jsonResponse);

                    var cpiData = JsonConvert.DeserializeObject<CPIdata>(jsonResponse);

                    if (cpiData == null || cpiData.Data == null || !cpiData.Data.Any())
                    {
                        throw new InvalidApiResponseException("Failed to retrieve valid CPI data.");
                    }

                    int currentYear = DateTime.Now.Year;
                    cpiData.Data = cpiData.Data.Where(d => DateTime.Parse(d.Date).Year == currentYear).ToList();

                    return cpiData;
                }
                else
                {
                    _logger.LogError("CPI data request unsuccessful. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"CPI data request unsuccessful. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while retrieving CPI data.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing CPI data.");
                throw new InvalidApiResponseException("JSON parsing error occurred.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving CPI data.");
                throw;
            }
        }

        public async Task<Inflation> GetInflationAsync()
        {
            string queryUrl = $"https://www.alphavantage.co/query?function=INFLATION&apikey={_apiKey}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(queryUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Received Inflation response: {JsonResponse}", jsonResponse);

                    var inflationData = JsonConvert.DeserializeObject<Inflation>(jsonResponse);

                    if (inflationData == null || inflationData.Data == null || !inflationData.Data.Any())
                    {
                        throw new InvalidApiResponseException("Failed to retrieve valid Inflation data.");
                    }

                    return inflationData;
                }
                else
                {
                    _logger.LogError("Inflation data request unsuccessful. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Inflation data request unsuccessful. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while retrieving Inflation data.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing Inflation data.");
                throw new InvalidApiResponseException("JSON parsing error occurred.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving Inflation data.");
                throw;
            }
        }

        public async Task<UnemploymentRate> GetUnemploymentRateAsync()
        {
            string queryUrl = $"https://www.alphavantage.co/query?function=UNEMPLOYMENT&apikey={_apiKey}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(queryUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Received Unemployment Rate response: {JsonResponse}", jsonResponse);

                    var unemploymentData = JsonConvert.DeserializeObject<UnemploymentRate>(jsonResponse);

                    if (unemploymentData == null || unemploymentData.Data == null || !unemploymentData.Data.Any())
                    {
                        throw new InvalidApiResponseException("Failed to retrieve valid Unemployment Rate data.");
                    }

                    return unemploymentData;
                }
                else
                {
                    _logger.LogError("Unemployment Rate data request unsuccessful. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Unemployment Rate data request unsuccessful. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while retrieving Unemployment Rate data.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing Unemployment Rate data.");
                throw new InvalidApiResponseException("JSON parsing error occurred.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving Unemployment Rate data.");
                throw;
            }
        }
        public async Task<FederalInterestRate> GetFederalInterestRateAsync()
        {
            string queryUrl = $"https://www.alphavantage.co/query?function=FEDERAL_FUNDS_RATE&interval=monthly&apikey={_apiKey}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(queryUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Received Federal Interest Rate response: {JsonResponse}", jsonResponse);

                    var federalRateData = JsonConvert.DeserializeObject<FederalInterestRate>(jsonResponse);

                    if (federalRateData == null || federalRateData.Data == null || !federalRateData.Data.Any())
                    {
                        throw new InvalidApiResponseException("Failed to retrieve valid Federal Interest Rate data.");
                    }

                    return federalRateData;
                }
                else
                {
                    _logger.LogError("Federal Interest Rate data request unsuccessful. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Federal Interest Rate data request unsuccessful. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while retrieving Federal Interest Rate data.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while processing Federal Interest Rate data.");
                throw new InvalidApiResponseException("JSON parsing error occurred.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving Federal Interest Rate data.");
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
