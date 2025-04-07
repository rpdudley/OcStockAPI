namespace XUnitTests.ActionTests;
public class AutoDeleteActionTests
{
    private DpapiDbContext _dbContext;
    private AutoDeleteAction _autoDeleteAction;
    private Mock<ILogger<AutoDeleteAction>> _loggerMock;

    public AutoDeleteActionTests()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _loggerMock = new Mock<ILogger<AutoDeleteAction>>();
        _autoDeleteAction = new AutoDeleteAction(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task DeleteOldStockHistoryAsync_DeletesOnlyRecordsOlderThan90Days()
    {
        var now = DateTime.UtcNow;
        _dbContext.StockHistories.AddRange(new List<StockHistory>
        {
            new StockHistory { HistoryId = 1, Timestamp = now.AddDays(-91), OpenedValue = 100, ClosedValue = 105, Volume = 1000, StockId = 1, Stock = new Stock { StockId = 1, Symbol = "AAPL" } },
            new StockHistory { HistoryId = 2, Timestamp = now.AddDays(-5), OpenedValue = 110, ClosedValue = 115, Volume = 2000, StockId = 2, Stock = new Stock { StockId = 2, Symbol = "MSFT" } }
        });

        await _dbContext.SaveChangesAsync();
        await _autoDeleteAction.DeleteOldStockHistoryAsync(CancellationToken.None);

        var remaining = await _dbContext.StockHistories.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(2, remaining.First().HistoryId);
    }

    [Fact]
    public async Task DeleteOldApiCallLogsAsync_DeletesOnlyRecordsOlderThan90Days()
    {
        var now = DateTime.UtcNow;
        _dbContext.ApiCallLog.AddRange(new List<ApiCallLog>
        {
            new ApiCallLog { Id = 1, Symbol = "AAPL", CallType = "Quote", CallDate = now.AddDays(-91) },
            new ApiCallLog { Id = 2, Symbol = "MSFT", CallType = "Quote", CallDate = now }
        });

        await _dbContext.SaveChangesAsync();
        await _autoDeleteAction.DeleteOldApiCallLogsAsync(CancellationToken.None);

        var remaining = await _dbContext.ApiCallLog.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(2, remaining.First().Id);
    }

    [Fact]
    public async Task DeleteOldStockHistoryAsync_DoesNotThrow_WhenNoneToDelete()
    {
        _dbContext.StockHistories.Add(new StockHistory
        {
            HistoryId = 3,
            Timestamp = DateTime.UtcNow,
            OpenedValue = 120,
            ClosedValue = 125,
            Volume = 1500,
            StockId = 3,
            Stock = new Stock { StockId = 3, Symbol = "GOOG" }
        });

        await _dbContext.SaveChangesAsync();
        var exception = await Record.ExceptionAsync(() => _autoDeleteAction.DeleteOldStockHistoryAsync(CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteOldApiCallLogsAsync_DoesNotThrow_WhenNoneToDelete()
    {
        _dbContext.ApiCallLog.Add(new ApiCallLog
        {
            Id = 3,
            Symbol = "TSLA",
            CallType = "News",
            CallDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        var exception = await Record.ExceptionAsync(() => _autoDeleteAction.DeleteOldApiCallLogsAsync(CancellationToken.None));
        Assert.Null(exception);
    }
}
