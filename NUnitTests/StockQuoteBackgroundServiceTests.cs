namespace NUnitTests;

public class StockQuoteBackgroundServiceTests
{
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILogger<StockQuoteBackgroundService>> _mockLogger;
    private Mock<IFinnhubService> _mockFinnhubService;
    private StockQuoteBackgroundService _service;

    [SetUp]
    public void Setup()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<StockQuoteBackgroundService>>();
        _mockFinnhubService = new Mock<IFinnhubService>();

        _service = new StockQuoteBackgroundService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockFinnhubService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
    }

    [Test]
    public async Task FetchAndSaveStockDataAsync_SavesStockAndHistory_WhenCalled()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new DpapiDbContext(options);
        var trackedStock = TestData.GetTrackedStock();
        dbContext.TrackedStocks.Add(trackedStock);
        await dbContext.SaveChangesAsync();

        var fakeQuote = TestData.GetFakeQuote();

        var mockAlphaVantage = new Mock<IAlphaVantageService>();
        mockAlphaVantage
            .Setup(x => x.GetStockQuoteAsync("AAPL"))
            .ReturnsAsync(fakeQuote);

        var mockApiLogger = new Mock<IApiRequestLogger>();

        // 💡 Fix: Proper service provider with mock AlphaVantageService
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAlphaVantageService)))
        .Returns(mockAlphaVantage.Object);

        // You must return this when _serviceProvider.GetRequiredService<IAlphaVantageService>() is called
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IAlphaVantageService)))
            .Returns(mockAlphaVantage.Object); // Optional fallback

        // Injecting service with the correct mock provider
        var service = new StockQuoteBackgroundService(
            mockServiceProvider.Object,
            Mock.Of<ILogger<StockQuoteBackgroundService>>(),
            Mock.Of<IFinnhubService>());

        var cancellationToken = new CancellationToken();

        // Act
        await service.FetchAndSaveStockDataAsync(dbContext, mockApiLogger.Object, "MarketClose", trackedStock, cancellationToken);

        // Assert
        var savedStock = await dbContext.Stocks.FirstOrDefaultAsync();
        Assert.NotNull(savedStock);
        Assert.AreEqual(185.50m, savedStock.ClosingValue);

        var savedHistory = await dbContext.StockHistories.FirstOrDefaultAsync();
        Assert.NotNull(savedHistory);
        Assert.AreEqual(180.00m, savedHistory.OpenedValue);
        Assert.AreEqual(185.50m, savedHistory.ClosedValue);

        mockApiLogger.Verify(x =>
            x.LogApiCallAsync("MarketClose", "AAPL", cancellationToken), Times.Once);
    }

    private static class TestData
    {
        public static TrackedStock GetTrackedStock() => new TrackedStock
        {
            Id = 1,
            Symbol = "AAPL",
            StockName = "Apple Inc."
        };

        public static StockQuote GetFakeQuote() => new StockQuote
        {
            Symbol = "AAPL",
            Open = 180.00m,
            Price = 185.50m,
            Volume = 2000000,
            LatestTradingDay = DateTime.UtcNow.Date
        };
    }
}