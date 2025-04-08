namespace NUnitTests;

[TestFixture]
public class AlphaVantageServiceTests
{
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private Mock<ILogger<AlphaVantageService>> _loggerMock;
    private AlphaVantageService _service;

    private readonly object sampleResponse = new
    {
        Global_Quote = new Dictionary<string, object>
        {
            ["01. symbol"] = "AAPL",
            ["02. open"] = 170.5m,
            ["03. high"] = 173.0m,
            ["04. low"] = 169.5m,
            ["05. price"] = 172.1m,
            ["06. volume"] = 5000000,
            ["07. latest trading day"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["08. previous close"] = 171.0m,
            ["10. change percent"] = "0.64%"
        }
    };

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _loggerMock = new Mock<ILogger<AlphaVantageService>>();

        var settings = new AlphaVantageSettings { ApiKey = "demo" };
        _service = new AlphaVantageService(_httpClient, settings, _loggerMock.Object);
    }

    [Test]
    public async Task GetStockQuoteAsync_Returns_ValidQuote()
    {
        var fixedJson = JsonConvert.SerializeObject(sampleResponse).Replace("Global_Quote", "Global Quote");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fixedJson)
            });

        var result = await _service.GetStockQuoteAsync("AAPL");

        Assert.IsNotNull(result);
        Assert.AreEqual("AAPL", result.Symbol);
    }

    [Test]
    public void GetStockQuoteAsync_Throws_OnApiError()
    {
        var errorResponse = new { ErrorMessage = "Invalid API call." };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(errorResponse))
            });

        Assert.ThrowsAsync<AlphaVantageService.InvalidApiResponseException>(() => _service.GetStockQuoteAsync("INVALID"));
    }
}