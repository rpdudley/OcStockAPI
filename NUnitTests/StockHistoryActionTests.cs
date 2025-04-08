namespace NUnitTests;
public class StockHistoryActionTests
{
    private DpapiDbContext _dbContext;
    private StockHistoryAction _stockHistoryAction;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        var stockAAPL = new Stock { StockId = 1, Symbol = "AAPL", Name = "Apple" };
        var stockGOOG = new Stock { StockId = 2, Symbol = "GOOG", Name = "Alphabet" };

        _dbContext.Stocks.AddRange(stockAAPL, stockGOOG);

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
                Stock = stockGOOG,
                Timestamp = new DateTime(2024, 12, 01),
                ClosedValue = 130
            }
        });

        _dbContext.SaveChanges();
        _stockHistoryAction = new StockHistoryAction(_dbContext);
    }

    [TearDown]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public void GetStockHistory_BySymbol_ReturnsCorrectData()
    {
        var result = _stockHistoryAction.GetStockHistory("AAPL");

        Assert.AreEqual(2, result.Count);
        Assert.That(result, Is.Ordered.Descending.By("Timestamp"));
        Assert.IsTrue(result.All(r => r.Stock.Symbol == "AAPL"));
    }

    [Test]
    public void GetStockHistory_BySymbolAndDateRange_ReturnsFiltered()
    {
        var from = new DateTime(2024, 12, 01);
        var to = new DateTime(2024, 12, 01);

        var result = _stockHistoryAction.GetStockHistory("AAPL", from, to);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DateTime(2024, 12, 01), result[0].Timestamp);
    }

    [Test]
    public void GetStockHistory_BySymbolAndDateRange_ReturnsEmpty_IfOutOfRange()
    {
        var result = _stockHistoryAction.GetStockHistory("AAPL", new DateTime(2023, 1, 1), new DateTime(2023, 12, 31));
        Assert.IsEmpty(result);
    }

    [Test]
    public void GetStockHistory_BySymbol_ReturnsEmpty_IfSymbolNotFound()
    {
        var result = _stockHistoryAction.GetStockHistory("TSLA");
        Assert.IsEmpty(result);
    }
}
