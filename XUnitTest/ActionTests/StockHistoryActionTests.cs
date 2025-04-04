using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace XUnitTests.ActionTests;

public class StockHistoryActionTests
{
    private DpapiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new DpapiDbContext(options);

        var aaplStock = new Stock { StockId = 1, Symbol = "AAPL", Name = "Apple Inc." };
        var googlStock = new Stock { StockId = 2, Symbol = "GOOGL", Name = "Alphabet Inc." };

        context.Stocks.AddRange(aaplStock, googlStock);
        context.StockHistories.AddRange(
            new StockHistory { HistoryId = 1, Stock = aaplStock, Timestamp = new DateTime(2025, 3, 15), OpenedValue = 150, ClosedValue = 155, Volume = 100000 },
            new StockHistory { HistoryId = 2, Stock = aaplStock, Timestamp = new DateTime(2025, 3, 10), OpenedValue = 148, ClosedValue = 153, Volume = 90000 },
            new StockHistory { HistoryId = 3, Stock = googlStock, Timestamp = new DateTime(2025, 3, 15), OpenedValue = 2800, ClosedValue = 2820, Volume = 110000 }
        );

        context.SaveChanges();
        return context;
    }

    [Fact]
    public void GetStockHistory_ReturnsAllForSymbol()
    {
        var dbContext = CreateDbContext();
        var action = new StockHistoryAction(dbContext);

        var result = action.GetStockHistory("AAPL");

        Assert.Equal(2, result.Count);
        Assert.All(result, sh => Assert.Equal("AAPL", sh.Stock.Symbol));
    }

    [Fact]
    public void GetStockHistory_WithDateRange_FiltersCorrectly()
    {
        var dbContext = CreateDbContext();
        var action = new StockHistoryAction(dbContext);
        var from = new DateTime(2025, 3, 12);
        var to = new DateTime(2025, 3, 16);

        var result = action.GetStockHistory("AAPL", from, to);

        Assert.Single(result);
        Assert.Equal(new DateTime(2025, 3, 15), result.First().Timestamp);
    }

    [Fact]
    public void GetStockHistory_ReturnsEmpty_WhenNoMatch()
    {
        var dbContext = CreateDbContext();
        var action = new StockHistoryAction(dbContext);

        var result = action.GetStockHistory("MSFT");

        Assert.Empty(result);
    }
}
