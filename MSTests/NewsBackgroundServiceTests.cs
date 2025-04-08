namespace MSTests;

[TestClass]
public class NewsBackgroundServiceTests
{
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<ILogger<NewsBackgroundService>> _loggerMock;
    private Mock<INewsAPIService> _newsServiceMock;
    private NewsBackgroundService _backgroundService;
    private DpapiDbContext _dbContext;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new DpapiDbContext(options);

        _dbContext.TrackedStocks.Add(new TrackedStock { Id = 1, Symbol = "TSLA", StockName = "Tesla" });
        _dbContext.Stocks.Add(new Stock { StockId = 100, Symbol = "TSLA", Name = "Tesla" });
        _dbContext.SaveChanges();

        _loggerMock = new Mock<ILogger<NewsBackgroundService>>();
        _newsServiceMock = new Mock<INewsAPIService>();

        _newsServiceMock.Setup(s => s.GetNewsDataAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Article>
            {
                new Article { Title = "Tesla News", Url = "http://example.com/article1", PublishedAt = DateTime.UtcNow }
            });

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);

        _serviceProviderMock.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(_scopeFactoryMock.Object);
        _serviceProviderMock.Setup(p => p.GetService(typeof(DpapiDbContext))).Returns(_dbContext);

        _backgroundService = new NewsBackgroundService(_serviceProviderMock.Object, _loggerMock.Object, _newsServiceMock.Object);
    }

    [TestMethod]
    public async Task FetchAndSaveNewsAsync_AddsNewsToDb()
    {
        await _backgroundService.FetchAndSaveNewsAsync(CancellationToken.None);

        var news = await _dbContext.MarketNews.ToListAsync();
        Assert.AreEqual(1, news.Count);
        Assert.AreEqual("Tesla News", news[0].Headline);
    }
}
