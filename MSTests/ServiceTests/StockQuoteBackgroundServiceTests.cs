namespace MSTests.ServicesTests;

[TestClass]
public class StockQuoteBackgroundServiceTests
{
    private DpapiDbContext _dbContext;
    private Mock<ILogger<StockQuoteBackgroundService>> _loggerMock;
    private Mock<IAlphaVantageService> _alphaServiceMock;
    private Mock<IApiRequestLogger> _apiLoggerMock;
    private Mock<IAutoDeleteService> _autoDeleteMock;
    private Mock<IFinnhubService> _finnhubServiceMock;
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new DpapiDbContext(options);

        _dbContext.TrackedStocks.Add(new TrackedStock { Id = 1, Symbol = "AAPL", StockName = "Apple Inc." });
        _dbContext.SaveChanges();

        _loggerMock = new Mock<ILogger<StockQuoteBackgroundService>>();
        _alphaServiceMock = new Mock<IAlphaVantageService>();
        _apiLoggerMock = new Mock<IApiRequestLogger>();
        _autoDeleteMock = new Mock<IAutoDeleteService>();
        _finnhubServiceMock = new Mock<IFinnhubService>();

        _finnhubServiceMock.Setup(f => f.MarkStatusAsync()).ReturnsAsync(new FinnhubMarketStatus { isOpen = false });
        _apiLoggerMock.Setup(x => x.HasMadeApiCallTodayAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _alphaServiceMock.Setup(x => x.GetStockQuoteAsync("AAPL")).ReturnsAsync(new StockQuote
        {
            Symbol = "AAPL",
            Open = 180.0m,
            Price = 185.0m,
            Volume = 5000000,
            LatestTradingDay = DateTime.UtcNow.Date
        });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_loggerMock.Object);
        serviceCollection.AddSingleton(_alphaServiceMock.Object);
        serviceCollection.AddSingleton(_apiLoggerMock.Object);
        serviceCollection.AddSingleton(_autoDeleteMock.Object);
        serviceCollection.AddSingleton(_finnhubServiceMock.Object);
        serviceCollection.AddSingleton(_dbContext);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [TestMethod]
    public async Task FetchAndSaveStockDataAsync_SavesCorrectData()
    {
        var service = new StockQuoteBackgroundService(_serviceProvider, _loggerMock.Object, _finnhubServiceMock.Object);

        var cancellationToken = new CancellationTokenSource().Token;

        var method = typeof(StockQuoteBackgroundService)
            .GetMethod("FetchAndSaveStockDataAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var trackedStock = await _dbContext.TrackedStocks.FirstAsync();

        await (Task)method.Invoke(service, new object[] { _dbContext, _apiLoggerMock.Object, "MarketClose", trackedStock, cancellationToken });

        var stock = await _dbContext.Stocks.FirstOrDefaultAsync();
        var history = await _dbContext.StockHistories.FirstOrDefaultAsync();

        Assert.IsNotNull(stock);
        Assert.AreEqual("AAPL", stock.Symbol);
        Assert.AreEqual(185.0m, stock.ClosingValue);

        Assert.IsNotNull(history);
        Assert.AreEqual(180.0m, history.OpenedValue);
        Assert.AreEqual(185.0m, history.ClosedValue);
    }
}