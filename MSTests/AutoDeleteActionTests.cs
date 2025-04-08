namespace MSTests;

[TestClass]
public class AutoDeleteActionTests
{
    private DpapiDbContext _dbContext;
    private Mock<ILogger<AutoDeleteAction>> _loggerMock;
    private AutoDeleteAction _service;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _loggerMock = new Mock<ILogger<AutoDeleteAction>>();

        _service = new AutoDeleteAction(_dbContext, _loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task DeleteOldStockHistoryAsync_DeletesOnlyOldRecords()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _dbContext.StockHistories.AddRange(
            new StockHistory { Timestamp = now.AddDays(-91), OpenedValue = 10, ClosedValue = 15, Volume = 1000 },
            new StockHistory { Timestamp = now, OpenedValue = 11, ClosedValue = 16, Volume = 1000 }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteOldStockHistoryAsync(CancellationToken.None);

        // Assert
        var remaining = await _dbContext.StockHistories.ToListAsync();
        Assert.AreEqual(1, remaining.Count);
        Assert.IsTrue(remaining.All(sh => sh.Timestamp >= now.AddDays(-90)));
    }

    [TestMethod]
    public async Task DeleteOldApiCallLogsAsync_DeletesOnlyOldLogs()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _dbContext.ApiCallLog.AddRange(
            new ApiCallLog { CallType = "Test", Symbol = "AAPL", CallDate = now.AddDays(-91) },
            new ApiCallLog { CallType = "Test", Symbol = "AAPL", CallDate = now }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteOldApiCallLogsAsync(CancellationToken.None);

        // Assert
        var remaining = await _dbContext.ApiCallLog.ToListAsync();
        Assert.AreEqual(1, remaining.Count);
        Assert.IsTrue(remaining.All(log => log.CallDate >= now.AddDays(-90)));
    }

    [TestMethod]
    public async Task DeleteOldStockHistoryAsync_HandlesCancellation()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
            _service.DeleteOldStockHistoryAsync(tokenSource.Token));
    }

    [TestMethod]
    public async Task DeleteOldApiCallLogsAsync_HandlesCancellation()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
            _service.DeleteOldApiCallLogsAsync(tokenSource.Token));
    }
}