namespace MSTests.ActionsTests;

[TestClass]
public class MarketNewsActionTests
{
    private DpapiDbContext _dbContext;
    private MarketNewsAction _marketNewsAction;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        var stock101 = new Stock { StockId = 101, Symbol = "AAPL", Name = "Apple" };
        var stock102 = new Stock { StockId = 102, Symbol = "TSLA", Name = "Tesla" };

        _dbContext.Stocks.AddRange(stock101, stock102);

        _dbContext.MarketNews.AddRange(new List<MarketNews>
        {
            new MarketNews
            {
                NewsId = 1,
                StockId = 101,
                Datetime = new DateTime(2025, 3, 30, 10, 0, 0),
                Headline = "Stock 101 rises",
                SourceUrl = "http://example.com/news1",
                Stock = stock101
            },
            new MarketNews
            {
                NewsId = 2,
                StockId = 101,
                Datetime = new DateTime(2025, 3, 30, 15, 0, 0),
                Headline = "Stock 101 spikes again",
                SourceUrl = "http://example.com/news2",
                Stock = stock101
            },
            new MarketNews
            {
                NewsId = 3,
                StockId = 102,
                Datetime = new DateTime(2025, 3, 30),
                Headline = "Stock 102 dips",
                SourceUrl = "http://example.com/news3",
                Stock = stock102
            },
            new MarketNews
            {
                NewsId = 4,
                StockId = 101,
                Datetime = new DateTime(2025, 3, 29),
                Headline = "Stock 101 preview",
                SourceUrl = "http://example.com/news4",
                Stock = stock101
            }
        });

        _dbContext.SaveChanges();
        _marketNewsAction = new MarketNewsAction(_dbContext);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task GetMarketNewsByDateAndStockId_ReturnsNewsForStockOnDate()
    {
        var result = await _marketNewsAction.GetMarketNewsByDateAndStockId(new DateTime(2025, 3, 30), 101);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[0].Datetime > result[1].Datetime); // Should be ordered descending
    }

    [TestMethod]
    public async Task GetMarketNewsByDateAndStockId_ReturnsEmpty_IfNoMatch()
    {
        var result = await _marketNewsAction.GetMarketNewsByDateAndStockId(new DateTime(2025, 3, 28), 101);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetMarketNewsByDateAndStockId_OnlyReturnsForCorrectStock()
    {
        var result = await _marketNewsAction.GetMarketNewsByDateAndStockId(new DateTime(2025, 3, 30), 102);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(102, result[0].StockId);
    }
}
