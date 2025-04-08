namespace XUnitTests;

public class NewsAPIServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly NewsAPIService _service;

    public NewsAPIServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object);
        var settings = new NewsSettings { ApiKey = "demo" };

        _service = new NewsAPIService(_httpClient, settings);
    }

    [Fact]
    public async Task GetNewsDataAsync_ReturnsArticles()
    {
        var articles = new List<Article>
        {
            new Article { Title = "Test Article", Url = "http://example.com", PublishedAt = DateTime.UtcNow }
        };

        var response = new NewsApiResponse { Articles = articles };
        var json = JsonConvert.SerializeObject(response);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var result = await _service.GetNewsDataAsync("apple", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        Assert.Single(result);
        Assert.Equal("Test Article", result[0].Title);
    }

    [Fact]
    public async Task GetNewsDataAsync_ThrowsOnError()
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid request")
            });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.GetNewsDataAsync("apple", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow));

        Assert.Contains("status code", ex.Message);
    }
}