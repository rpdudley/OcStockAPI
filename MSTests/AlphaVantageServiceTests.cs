namespace MSTests;

[TestClass]
public class AlphaVantageServiceTests
{
    private AlphaVantageService _service;
    private Mock<HttpMessageHandler> _handlerMock;
    private Mock<ILogger<AlphaVantageService>> _loggerMock;
    private HttpClient _httpClient;

    [TestInitialize]
    public void Setup()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<AlphaVantageService>>();

        _httpClient = new HttpClient(_handlerMock.Object);
        var settings = new AlphaVantageSettings { ApiKey = "demo" };

        _service = new AlphaVantageService(_httpClient, settings, _loggerMock.Object);
    }

    [TestMethod]
    public async Task GetStockQuoteAsync_ReturnsValidQuote()
    {
        var json = JsonConvert.SerializeObject(new Dictionary<string, object>
        {
            ["Global Quote"] = new Dictionary<string, string>
            {
                { "01. symbol", "AAPL" },
                { "02. open", "150.00" },
                { "05. price", "155.00" },
                { "06. volume", "1000000" },
                { "07. latest trading day", "2025-03-31" }
            }
        });

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var quote = await _service.GetStockQuoteAsync("AAPL");

        Assert.IsNotNull(quote);
        Assert.AreEqual("AAPL", quote.Symbol);
        Assert.AreEqual(150.00m, quote.Open);
        Assert.AreEqual(155.00m, quote.Price);
        Assert.AreEqual(1000000, quote.Volume);
        Assert.AreEqual(new DateTime(2025, 3, 31), quote.LatestTradingDay);
    }

    [TestMethod]
    public async Task GetStockQuoteAsync_ThrowsOnRateLimitNote()
    {
        var rateLimitJson = JsonConvert.SerializeObject(new { Note = "Too many requests" });

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(rateLimitJson)
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        await Assert.ThrowsExceptionAsync<AlphaVantageService.ApiRateLimitExceededException>(() =>
            _service.GetStockQuoteAsync("AAPL"));
    }

    [TestMethod]
    public async Task GetStockQuoteAsync_ThrowsOnErrorMessage()
    {
        var errorJson = JsonConvert.SerializeObject(new { ErrorMessage = "Invalid API call" });

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(errorJson)
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        await Assert.ThrowsExceptionAsync<AlphaVantageService.InvalidApiResponseException>(() =>
            _service.GetStockQuoteAsync("INVALID"));
    }
}