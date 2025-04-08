namespace NUnitTests;

[TestFixture]
public class FinnhubServiceTests
{
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private HttpClient _httpClient;
    private FinnhubService _service;

    [SetUp]
    public void Setup()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        var settings = new FinnhubSettings { ApiKey = "demo" };
        _service = new FinnhubService(_httpClient, settings);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task GetStockDataAsync_Returns_JObject()
    {
        var responseContent = new JObject
        {
            ["s"] = "ok",
            ["c"] = new JArray(172.1),
            ["t"] = new JArray(1617280000)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent.ToString())
            });

        var result = await _service.GetStockDataAsync("AAPL", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        Assert.IsNotNull(result);
        Assert.That(result["s"].ToString(), Is.EqualTo("ok"));
    }

    [Test]
    public async Task MarkStatusAsync_Returns_MarketStatus()
    {
        var marketStatus = new FinnhubMarketStatus
        {
            isOpen = true,
            t = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(marketStatus))
            });

        var result = await _service.MarkStatusAsync();

        Assert.IsTrue(result.isOpen);
    }
}
