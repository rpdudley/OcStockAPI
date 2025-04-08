namespace MSTests;

[TestClass]
public class FinnhubServiceTests
{
    private FinnhubService _service;
    private Mock<HttpMessageHandler> _handlerMock;
    private HttpClient _httpClient;

    [TestInitialize]
    public void Setup()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);

        var settings = new FinnhubSettings { ApiKey = "demo" };
        _service = new FinnhubService(_httpClient, settings);
    }

    [TestMethod]
    public async Task GetStockDataAsync_ReturnsParsedJson()
    {
        var jsonResponse = new
        {
            c = new[] { 150.0, 152.0 },
            t = new[] { 1640995200, 1641081600 },
            s = "ok"
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(jsonResponse))
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var fromDate = new DateTime(2022, 1, 1);
        var toDate = new DateTime(2022, 1, 2);

        var result = await _service.GetStockDataAsync("AAPL", fromDate, toDate);

        Assert.IsNotNull(result);
        Assert.AreEqual("ok", result["s"].ToString());
    }

    [TestMethod]
    public async Task MarkStatusAsync_ReturnsMarketStatusObject()
    {
        var json = JsonConvert.SerializeObject(new FinnhubMarketStatus
        {
            exchange = "US",
            isOpen = true
        });

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var result = await _service.MarkStatusAsync();

        Assert.IsNotNull(result);
        Assert.IsTrue(result.isOpen);
        Assert.AreEqual("US", result.exchange);
    }
}
