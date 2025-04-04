namespace MSTests.ActionsTests;

[TestClass]
public class StockActionTests
{
    private DpapiDbContext _dbContext;
    private StockAction _stockAction;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        _dbContext.Stocks.AddRange(new List<Stock>
        {
            new Stock { TrackedStockId = 1, Symbol = "AAPL", Name = "Apple Inc." },
            new Stock { TrackedStockId = 2, Symbol = "GOOGL", Name = "Alphabet Inc." },
            new Stock { TrackedStockId = 3, Symbol = "AAPL", Name = "Apple Holdings" }
        });

        _dbContext.SaveChanges();
        _stockAction = new StockAction(_dbContext);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task GetStocksById_ReturnsCorrectStock()
    {
        var stock = await _stockAction.GetStocksById(1);

        Assert.IsNotNull(stock);
        Assert.AreEqual("AAPL", stock.Symbol);
        Assert.AreEqual("Apple Inc.", stock.Name);
    }

    [TestMethod]
    public async Task GetStocksById_ReturnsNull_IfNotFound()
    {
        var stock = await _stockAction.GetStocksById(999);
        Assert.IsNull(stock);
    }

    [TestMethod]
    public async Task GetAllStocks_ReturnsAllStocks()
    {
        var stocks = await _stockAction.GetAllStocks();
        Assert.AreEqual(3, stocks.Count);
    }

    [TestMethod]
    public async Task GetStocksBySymbol_ReturnsCorrectStocks()
    {
        var stocks = await _stockAction.GetStocksBySymbol("AAPL");

        Assert.AreEqual(2, stocks.Count);
        Assert.IsTrue(stocks.All(s => s.Symbol == "AAPL"));
    }

    [TestMethod]
    public async Task GetStocksBySymbol_ReturnsEmptyList_IfNoMatch()
    {
        var stocks = await _stockAction.GetStocksBySymbol("MSFT");
        Assert.AreEqual(0, stocks.Count);
    }
}
