namespace NUnitTests.ActionsTests;

public class MarketNewsActionTests
{
    private DpapiDbContext _dbContext;
    private MarketNewsAction _marketNewsAction;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        // Create one instance of each stock
        var stock101 = new Stock { StockId = 101, Symbol = "TEST101", Name = "Test Stock 101" };
        var stock102 = new Stock { StockId = 102, Symbol = "TEST102", Name = "Test Stock 102" };

        _dbContext.Stocks.AddRange(stock101, stock102);

        _dbContext.MarketNews.AddRange(new List<MarketNews>
    {
        new MarketNews
        {
            NewsId = 1,
            Datetime = new DateTime(2025, 3, 30, 10, 0, 0),
            StockId = 101,
            Headline = "Stock 101 rises",
            SourceUrl = "http://example.com/news1",
            Stock = stock101
        },
        new MarketNews
        {
            NewsId = 2,
            Datetime = new DateTime(2025, 3, 30, 15, 0, 0),
            StockId = 101,
            Headline = "Stock 101 spikes again",
            SourceUrl = "http://example.com/news2",
            Stock = stock101
        },
        new MarketNews
        {
            NewsId = 3,
            Datetime = new DateTime(2025, 3, 30),
            StockId = 102,
            Headline = "Stock 102 dips",
            SourceUrl = "http://example.com/news3",
            Stock = stock102
        },
        new MarketNews
        {
            NewsId = 4,
            Datetime = new DateTime(2025, 3, 29),
            StockId = 101,
            Headline = "Stock 101 preview",
            SourceUrl = "http://example.com/news4",
            Stock = stock101
        }
    });

        _dbContext.SaveChanges();
        _marketNewsAction = new MarketNewsAction(_dbContext);
    }


    [TearDown]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetMarketNewsByDateAndStockId_ReturnsCorrectItems()
    {
        var result = await _marketNewsAction.GetMarketNewsByDateAndStockId(new DateTime(2025, 3, 30), 101);

        Assert.That(result.Count, Is.EqualTo(2)); // Two entries for StockId 101 on 3/30
        Assert.That(result[0].Datetime, Is.GreaterThan(result[1].Datetime)); // Ordered descending
    }

    [Test]
    public async Task GetMarketNewsByDateAndStockId_ReturnsEmptyList_IfNoMatch()
    {
        var result = await _marketNewsAction.GetMarketNewsByDateAndStockId(new DateTime(2025, 3, 28), 101);
        Assert.IsEmpty(result);
    }

    [Test]
    public async Task GetMarketNewsByDateAndStockId_ReturnsOnlyForSpecificStock()
    {
        var result = await _marketNewsAction.GetMarketNewsByDateAndStockId(new DateTime(2025, 3, 30), 102);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].StockId, Is.EqualTo(102));
        Assert.That(result[0].Headline, Is.EqualTo("Stock 102 dips"));
    }
}
