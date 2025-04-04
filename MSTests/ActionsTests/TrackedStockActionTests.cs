namespace MSTests.ActionsTests;

[TestClass]
public class TrackedStockActionTests
{
    private DpapiDbContext _dbContext;
    private TrackedStockAction _trackedStockAction;

    [TestInitialize]
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

    [TestCleanup]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public void GetTrackedStocks_ReturnsAll_WhenNoSymbolProvided()
    {
        var result = _trackedStockAction.GetTrackedStocks();
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public void GetTrackedStocks_ReturnsFiltered_WhenSymbolPrefixProvided()
    {
        var result = _trackedStockAction.GetTrackedStocks("A");

        Assert.AreEqual(2, result.Count); // AAPL and AMZN
        Assert.IsTrue(result.All(s => s.Symbol.StartsWith("A")));
    }

    [TestMethod]
    public void AddTrackedStock_AddsNew_WhenNotExists()
    {
        var newStock = new TrackedStock { Symbol = "MSFT", StockName = "Microsoft" };

        _trackedStockAction.AddTrackedStock(newStock);

        var all = _trackedStockAction.GetTrackedStocks();
        Assert.AreEqual(4, all.Count);
        Assert.IsTrue(all.Any(s => s.Symbol == "MSFT"));
    }

    [TestMethod]
    public void AddTrackedStock_DoesNotAddDuplicate()
    {
        var duplicate = new TrackedStock { Symbol = "AAPL", StockName = "Apple Copy" };

        _trackedStockAction.AddTrackedStock(duplicate);

        var aaplStocks = _trackedStockAction.GetTrackedStocks("AAPL");
        Assert.AreEqual(1, aaplStocks.Count); // Only one AAPL
        Assert.AreEqual("Apple Inc.", aaplStocks[0].StockName); // Original remains
    }
}
