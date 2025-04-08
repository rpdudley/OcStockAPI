namespace XUnitTests;
public class FinnhubServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly FinnhubService _service;

    public FinnhubServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);

        var settings = new FinnhubSettings
        {
            ApiKey = "test-api-key"
        };

        _service = new FinnhubService(_httpClient, settings);
    }

    [Fact]
    public async Task GetStockDataAsync_ReturnsParsedJObject()
    {
        var responseContent = new
        {
            c = new[] { 150.1, 151.2 },
            t = new[] { 1617753600, 1617840000 }
        };

        var json = JsonConvert.SerializeObject(responseContent);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var result = await _service.GetStockDataAsync("AAPL", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("c"));
    }

    [Fact]
    public async Task MarkStatusAsync_ReturnsDeserializedStatus()
    {
        var marketStatus = new FinnhubMarketStatus
        {
            isOpen = false,
            exchange = "US"
        };

        var json = JsonConvert.SerializeObject(marketStatus);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var result = await _service.MarkStatusAsync();

        Assert.NotNull(result);
        Assert.False(result.isOpen);
        Assert.Equal("US", result.exchange);
    }
}