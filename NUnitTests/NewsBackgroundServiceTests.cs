namespace NUnitTests;

public class NewsAPIServiceTests
{
    private NewsAPIService _service;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        var settings = new NewsSettings
        {
            ApiKey = "FAKE_API_KEY"
        };

        _service = new NewsAPIService(httpClient, settings);
    }

    [Test]
    public async Task GetNewsDataAsync_ReturnsArticles_WhenApiRespondsSuccessfully()
    {
        // Arrange
        var expectedArticles = new List<Article>
        {
            new Article { Title = "Test Article 1", Url = "http://example.com/1", PublishedAt = DateTime.UtcNow },
            new Article { Title = "Test Article 2", Url = "http://example.com/2", PublishedAt = DateTime.UtcNow }
        };

        var fakeResponse = new NewsApiResponse
        {
            Articles = expectedArticles
        };

        var jsonResponse = JsonConvert.SerializeObject(fakeResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetNewsDataAsync("Apple", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Test Article 1", result[0].Title);
    }

    [Test]
    public void GetNewsDataAsync_ThrowsException_WhenApiFails()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad request")
            });

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _service.GetNewsDataAsync("Fake", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow));

        StringAssert.Contains("Request to News API failed", ex.Message);
    }
}