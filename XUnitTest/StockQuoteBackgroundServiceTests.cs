namespace XUnitTests;
public class StockQuoteBackgroundServiceTests
{
    [Fact]
    public async Task FetchAndSaveStockDataAsync_SavesStockAndHistory_WhenCalled()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new DpapiDbContext(options);
        var trackedStock = new TrackedStock { Id = 1, Symbol = "AAPL", StockName = "Apple Inc." };
        dbContext.TrackedStocks.Add(trackedStock);
        await dbContext.SaveChangesAsync();

        var fakeQuote = new StockQuote
        {
            Symbol = "AAPL",
            Open = 180.00m,
            Price = 185.50m,
            Volume = 2000000,
            LatestTradingDay = DateTime.UtcNow.Date
        };

        var mockAlphaVantage = new Mock<IAlphaVantageService>();
        mockAlphaVantage.Setup(x => x.GetStockQuoteAsync("AAPL")).ReturnsAsync(fakeQuote);

        var mockApiLogger = new Mock<IApiRequestLogger>();

        var mockLogger = new Mock<ILogger<StockQuoteBackgroundService>>();
        var mockFinnhubService = new Mock<IFinnhubService>();

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IAlphaVantageService)))
                            .Returns(mockAlphaVantage.Object);

        var service = new StockQuoteBackgroundService(
            mockServiceProvider.Object,
            mockLogger.Object,
            mockFinnhubService.Object
        );

        var cancellationToken = new CancellationToken();

        // Act
        await service.FetchAndSaveStockDataAsync(dbContext, mockApiLogger.Object, "MarketClose", trackedStock, cancellationToken);

        // Assert
        var savedStock = await dbContext.Stocks.FirstOrDefaultAsync();
        Assert.NotNull(savedStock);
        Assert.Equal(185.50m, savedStock.ClosingValue);

        var savedHistory = await dbContext.StockHistories.FirstOrDefaultAsync();
        Assert.NotNull(savedHistory);
        Assert.Equal(180.00m, savedHistory.OpenedValue);
        Assert.Equal(185.50m, savedHistory.ClosedValue);
        Assert.Equal(savedStock.StockId, savedHistory.Stock.StockId);

        mockApiLogger.Verify(x =>
            x.LogApiCallAsync("MarketClose", "AAPL", cancellationToken), Times.Once);
    }
}