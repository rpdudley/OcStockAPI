namespace XUnitTests.ActionTests;
public class MarketNewsActionTests
{
    private DpapiDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new DpapiDbContext(options);

        dbContext.MarketNews.AddRange(new List<MarketNews>
        {
            new MarketNews
            {
                NewsId = 1,
                StockId = 101,
                Datetime = new DateTime(2025, 4, 1, 10, 0, 0),
                Headline = "Stock 101 rises",
                SourceUrl = "http://example.com/1"
            },
            new MarketNews
            {
                NewsId = 2,
                StockId = 101,
                Datetime = new DateTime(2025, 4, 1, 15, 0, 0),
                Headline = "Stock 101 jumps again",
                SourceUrl = "http://example.com/2"
            },
            new MarketNews
            {
                NewsId = 3,
                StockId = 102,
                Datetime = new DateTime(2025, 4, 1),
                Headline = "Stock 102 drops",
                SourceUrl = "http://example.com/3"
            }
        });

        dbContext.SaveChanges();
        return dbContext;
    }

    [Fact]
    public async Task GetMarketNewsByDateAndStockId_ReturnsMatchingNews()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var action = new MarketNewsAction(dbContext);

        // Act
        var result = await action.GetMarketNewsByDateAndStockId(new DateTime(2025, 4, 1), 101);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(101, r.StockId));
        Assert.True(result[0].Datetime > result[1].Datetime); // check ordering
    }

    [Fact]
    public async Task GetMarketNewsByDateAndStockId_ReturnsEmpty_WhenNoMatch()
    {
        var dbContext = GetInMemoryDbContext();
        var action = new MarketNewsAction(dbContext);

        var result = await action.GetMarketNewsByDateAndStockId(new DateTime(2025, 3, 31), 101);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMarketNewsByDateAndStockId_FiltersByStockId()
    {
        var dbContext = GetInMemoryDbContext();
        var action = new MarketNewsAction(dbContext);

        var result = await action.GetMarketNewsByDateAndStockId(new DateTime(2025, 4, 1), 102);

        Assert.Single(result);
        Assert.Equal("Stock 102 drops", result[0].Headline);
    }
}
