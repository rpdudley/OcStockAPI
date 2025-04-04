using DatabaseProjectAPI.Services;
using KubsConnect.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace XUnitTests.ServiceTests
{
    public class AlphaVantageServiceTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly Mock<ILogger<AlphaVantageService>> _loggerMock;
        private readonly HttpClient _httpClient;
        private readonly AlphaVantageService _service;

        public AlphaVantageServiceTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<AlphaVantageService>>();

            _httpClient = new HttpClient(_handlerMock.Object);
            var settings = new AlphaVantageSettings { ApiKey = "demo" };

            _service = new AlphaVantageService(_httpClient, settings, _loggerMock.Object);
        }

        [Fact]
        public async Task GetStockQuoteAsync_ReturnsValidStockQuote()
        {
            var today = DateTime.UtcNow.Date;

            var responseObject = new Dictionary<string, object>
            {
                ["Global Quote"] = new Dictionary<string, object>
                {
                    ["01. symbol"] = "AAPL",
                    ["02. open"] = "170.5",
                    ["03. high"] = "173.0",
                    ["04. low"] = "169.5",
                    ["05. price"] = "172.1",
                    ["06. volume"] = "5000000",
                    ["07. latest trading day"] = today.ToString("yyyy-MM-dd"),
                    ["08. previous close"] = "171.0",
                    ["10. change percent"] = "0.64%"
                }
            };

            var responseContent = JsonConvert.SerializeObject(responseObject);

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                });

            var result = await _service.GetStockQuoteAsync("AAPL");

            Assert.NotNull(result);
            Assert.Equal("AAPL", result.Symbol);
            Assert.Equal(170.5m, result.Open);
            Assert.Equal(172.1m, result.Price);
            Assert.Equal(5000000, result.Volume);
            Assert.Equal(today, result.LatestTradingDay.Date);
        }


        [Fact]
        public async Task GetStockQuoteAsync_ThrowsOnRateLimit()
        {
            var rateLimitResponse = JsonConvert.SerializeObject(new { Note = "Rate limit exceeded" });

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(rateLimitResponse)
                });

            await Assert.ThrowsAsync<AlphaVantageService.ApiRateLimitExceededException>(() => _service.GetStockQuoteAsync("AAPL"));
        }

        [Fact]
        public async Task GetStockQuoteAsync_ThrowsOnErrorMessage()
        {
            var errorResponse = JsonConvert.SerializeObject(new { ErrorMessage = "Invalid API call" });

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(errorResponse)
                });

            await Assert.ThrowsAsync<AlphaVantageService.InvalidApiResponseException>(() => _service.GetStockQuoteAsync("INVALID"));
        }
    }
}
