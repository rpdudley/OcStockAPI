namespace XUnitTests.ActionTests;
public class StockActionTests
{
    private DpapiDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new DpapiDbContext(options);

        context.Stocks.AddRange(new List<Stock>
        {
            new Stock { StockId = 1, TrackedStockId = 101, Symbol = "AAPL", Name = "Apple" },
            new Stock { StockId = 2, TrackedStockId = 102, Symbol = "GOOGL", Name = "Google" },
            new Stock { StockId = 3, TrackedStockId = 103, Symbol = "AAPL", Name = "Apple Duplicate" }
        });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GetStocksById_ReturnsCorrectStock()
    {
        var context = GetDbContext();
        var action = new StockAction(context);

        var stock = await action.GetStocksById(101);

        Assert.NotNull(stock);
        Assert.Equal("AAPL", stock.Symbol);
        Assert.Equal("Apple", stock.Name);
    }

    [Fact]
    public async Task GetAllStocks_ReturnsAllStocks()
    {
        var context = GetDbContext();
        var action = new StockAction(context);

        var stocks = await action.GetAllStocks();

        Assert.Equal(3, stocks.Count);
    }

    [Fact]
    public async Task GetStocksBySymbol_ReturnsMatchingStocks()
    {
        var context = GetDbContext();
        var action = new StockAction(context);

        var stocks = await action.GetStocksBySymbol("AAPL");

        Assert.Equal(2, stocks.Count);
        Assert.All(stocks, s => Assert.Equal("AAPL", s.Symbol));
    }

    [Fact]
    public async Task GetStocksBySymbol_ReturnsEmptyList_IfNoMatch()
    {
        var context = GetDbContext();
        var action = new StockAction(context);

        var stocks = await action.GetStocksBySymbol("MSFT");

        Assert.Empty(stocks);
    }
}
