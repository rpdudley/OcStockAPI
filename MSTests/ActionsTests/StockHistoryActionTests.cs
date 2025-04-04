namespace MSTests.ActionsTests;

[TestClass]
public class StockHistoryActionTests
{
    private DpapiDbContext _dbContext;
    private StockHistoryAction _stockHistoryAction;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        var stockAAPL = new Stock { StockId = 1, Symbol = "AAPL", Name = "Apple" };
        var stockTSLA = new Stock { StockId = 2, Symbol = "TSLA", Name = "Tesla" };

        _dbContext.Stocks.AddRange(stockAAPL, stockTSLA);

        _dbContext.StockHistories.AddRange(new List<StockHistory>
        {
            new StockHistory
            {
                HistoryId = 1,
                Stock = stockAAPL,
                Timestamp = new DateTime(2024, 12, 01),
                ClosedValue = 150
            },
            new StockHistory
            {
                HistoryId = 2,
                Stock = stockAAPL,
                Timestamp = new DateTime(2024, 12, 02),
                ClosedValue = 152
            },
            new StockHistory
            {
                HistoryId = 3,
                Stock = stockTSLA,
                Timestamp = new DateTime(2024, 12, 01),
                ClosedValue = 680
            }
        });

        _dbContext.SaveChanges();
        _stockHistoryAction = new StockHistoryAction(_dbContext);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public void GetStockHistory_BySymbol_ReturnsCorrectRecords()
    {
        var result = _stockHistoryAction.GetStockHistory("AAPL");

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(h => h.Stock.Symbol == "AAPL"));
        Assert.IsTrue(result[0].Timestamp > result[1].Timestamp);
    }

    [TestMethod]
    public void GetStockHistory_ByDateRange_ReturnsFilteredRecords()
    {
        var from = new DateTime(2024, 12, 01);
        var to = new DateTime(2024, 12, 01);

        var result = _stockHistoryAction.GetStockHistory("AAPL", from, to);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DateTime(2024, 12, 01), result[0].Timestamp);
    }

    [TestMethod]
    public void GetStockHistory_ReturnsEmpty_IfSymbolNotFound()
    {
        var result = _stockHistoryAction.GetStockHistory("MSFT");
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetStockHistory_ByDateRange_ReturnsEmpty_IfOutOfRange()
    {
        var result = _stockHistoryAction.GetStockHistory("AAPL", new DateTime(2020, 1, 1), new DateTime(2020, 1, 2));
        Assert.AreEqual(0, result.Count);
    }
}
