namespace NUnitTests.ActionsTests;

public class TrackedStockActionTests
{
    private DpapiDbContext _dbContext;
    private TrackedStockAction _trackedStockAction;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public void GetTrackedStocks_ReturnsAll_WhenNoSymbolProvided()
    {
        var result = _trackedStockAction.GetTrackedStocks();
        Assert.AreEqual(3, result.Count);
    }

    [Test]
    public void GetTrackedStocks_ReturnsFilteredList_WhenSymbolProvided()
    {
        var result = _trackedStockAction.GetTrackedStocks("A");

        Assert.AreEqual(2, result.Count); // AAPL and AMZN
        Assert.IsTrue(result.All(s => s.Symbol != null && s.Symbol.StartsWith("A")));
    }

    [Test]
    public void AddTrackedStock_AddsNewStock_WhenNotExists()
    {
        var newStock = new TrackedStock { Symbol = "MSFT", StockName = "Microsoft" };

        _trackedStockAction.AddTrackedStock(newStock);

        var all = _trackedStockAction.GetTrackedStocks();
        Assert.AreEqual(4, all.Count);
        Assert.IsTrue(all.Any(s => s.Symbol == "MSFT"));
    }

    [Test]
    public void AddTrackedStock_DoesNotAddDuplicate()
    {
        var duplicateStock = new TrackedStock { Symbol = "AAPL", StockName = "Apple Duplicate" };

        _trackedStockAction.AddTrackedStock(duplicateStock);

        var result = _trackedStockAction.GetTrackedStocks("AAPL");
        Assert.AreEqual(1, result.Count); // No duplicate added
        Assert.AreEqual("Apple Inc.", result[0].StockName);
    }
}