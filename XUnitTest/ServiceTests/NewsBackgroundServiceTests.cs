namespace XUnitTests.ServiceTests;
public class NewsBackgroundServiceTests
{
    [Fact]
    public async Task FetchAndSaveNewsAsync_SavesNews_WhenValidResponse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new DpapiDbContext(options);

        var stock = new Stock { StockId = 1, Symbol = "AAPL", Name = "Apple Inc." };
        var trackedStock = new TrackedStock { Id = 1, Symbol = "AAPL", StockName = "Apple Inc." };

        dbContext.Stocks.Add(stock);
        dbContext.TrackedStocks.Add(trackedStock);
        await dbContext.SaveChangesAsync();

        var mockNewsService = new Mock<INewsAPIService>();
        mockNewsService.Setup(x => x.GetNewsDataAsync("Apple Inc.", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Article>
            {
                new Article
                {
                    Title = "Apple releases new iPhone",
                    Url = "https://example.com/apple-news",
                    PublishedAt = DateTime.UtcNow
                }
            });

        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        var scopedServiceProviderMock = new Mock<IServiceProvider>();

        scopedServiceProviderMock.Setup(x => x.GetService(typeof(DpapiDbContext))).Returns(dbContext);
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedServiceProviderMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var loggerMock = new Mock<ILogger<NewsBackgroundService>>();
        var service = new NewsBackgroundService(serviceProviderMock.Object, loggerMock.Object, mockNewsService.Object);

        // Act
        await service.FetchAndSaveNewsAsync(CancellationToken.None);

        // Assert
        Assert.Single(dbContext.MarketNews);
        var savedNews = dbContext.MarketNews.First();
        Assert.Equal("Apple releases new iPhone", savedNews.Headline);
    }
}
