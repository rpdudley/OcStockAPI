namespace MSTests.ServicesTests;

[TestClass]
public class NewsAPIServiceTests
{
    private Mock<HttpMessageHandler> _handlerMock;
    private HttpClient _httpClient;
    private NewsAPIService _service;

    [TestInitialize]
    public void Setup()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);
        var settings = new NewsSettings { ApiKey = "demo" };
        _service = new NewsAPIService(_httpClient, settings);
    }

    [TestMethod]
    public async Task GetNewsDataAsync_ReturnsArticleList()
    {
        var articles = new List<Article>
        {
            new Article { Title = "Stock Market News", Url = "http://news.com/article1" },
            new Article { Title = "Another News", Url = "http://news.com/article2" }
        };

        var jsonResponse = JsonConvert.SerializeObject(new NewsApiResponse
        {
            Articles = articles
        });

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var fromDate = DateTime.UtcNow.AddDays(-2);
        var toDate = DateTime.UtcNow;

        var result = await _service.GetNewsDataAsync("Tesla", fromDate, toDate);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Stock Market News", result[0].Title);
    }

    [TestMethod]
    [ExpectedException(typeof(HttpRequestException))]
    public async Task GetNewsDataAsync_ThrowsOnBadStatusCode()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("Bad Request")
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        var fromDate = DateTime.UtcNow.AddDays(-2);
        var toDate = DateTime.UtcNow;

        await _service.GetNewsDataAsync("Tesla", fromDate, toDate);
    }
}
