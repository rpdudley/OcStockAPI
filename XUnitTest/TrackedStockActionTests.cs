namespace XUnitTests;
public class TrackedStockActionTests
{
    private DpapiDbContext _dbContext;
    private TrackedStockAction _trackedStockAction;

    public TrackedStockActionTests()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _dbContext.TrackedStocks.AddRange(new List<TrackedStock>
        {
            new TrackedStock { Id = 1, Symbol = "AAPL", StockName = "Apple Inc." },
            new TrackedStock { Id = 2, Symbol = "AMZN", StockName = "Amazon Inc." },
            new TrackedStock { Id = 3, Symbol = "GOOGL", StockName = "Alphabet Inc." }
        });
        _dbContext.SaveChanges();

        _trackedStockAction = new TrackedStockAction(_dbContext);
    }

    [Fact]
    public void GetTrackedStocks_ReturnsAll_WhenNoSymbolProvided()
    {
        var result = _trackedStockAction.GetTrackedStocks();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetTrackedStocks_ReturnsFilteredList_WhenSymbolProvided()
    {
        var result = _trackedStockAction.GetTrackedStocks("A");

        Assert.Equal(2, result.Count); // AAPL and AMZN
        Assert.All(result, s => Assert.StartsWith("A", s.Symbol));
    }

    [Fact]
    public void AddTrackedStock_AddsNewStock_WhenNotExists()
    {
        var newStock = new TrackedStock { Symbol = "MSFT", StockName = "Microsoft" };

        _trackedStockAction.AddTrackedStock(newStock);

        var all = _trackedStockAction.GetTrackedStocks();
        Assert.Equal(4, all.Count);
        Assert.Contains(all, s => s.Symbol == "MSFT");
    }

    [Fact]
    public void AddTrackedStock_DoesNotAddDuplicate()
    {
        var duplicateStock = new TrackedStock { Symbol = "AAPL", StockName = "Apple Duplicate" };

        _trackedStockAction.AddTrackedStock(duplicateStock);

        var result = _trackedStockAction.GetTrackedStocks("AAPL");
        Assert.Single(result);
        Assert.Equal("Apple Inc.", result[0].StockName);
    }
}
