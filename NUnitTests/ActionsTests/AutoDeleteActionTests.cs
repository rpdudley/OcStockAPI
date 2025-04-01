namespace NUnitTests.ActionsTests;

public class AutoDeleteActionTests
{
    private DpapiDbContext _dbContext;
    private AutoDeleteAction _autoDeleteAction;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        // Add StockHistory: 1 old, 1 recent
        _dbContext.StockHistories.AddRange(new List<StockHistory>
        {
            new StockHistory
            {
                HistoryId = 1,
                StockId = 1,
                Timestamp = DateTime.UtcNow.AddDays(-91),
                ClosedValue = 100
            },
            new StockHistory
            {
                HistoryId = 2,
                StockId = 1,
                Timestamp = DateTime.UtcNow.AddDays(-10),
                ClosedValue = 150
            }
        });

        // Add ApiCallLogs: 1 old, 1 recent
        _dbContext.ApiCallLog.AddRange(new List<ApiCallLog>
        {
            new ApiCallLog
            {
                Id = 1,
                Symbol = "AAPL",
                CallType = "Quote",
                CallDate = DateTime.UtcNow.AddDays(-91)
            },
            new ApiCallLog
            {
                Id = 2,
                Symbol = "AAPL",
                CallType = "Quote",
                CallDate = DateTime.UtcNow.AddDays(-10)
            }
        });

        _dbContext.SaveChanges();

        var logger = new LoggerFactory().CreateLogger<AutoDeleteAction>();
        _autoDeleteAction = new AutoDeleteAction(_dbContext, logger);
    }

    [TearDown]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task DeleteOldStockHistoryAsync_DeletesOnlyRecordsOlderThan90Days()
    {
        await _autoDeleteAction.DeleteOldStockHistoryAsync(CancellationToken.None);

        var remaining = await _dbContext.StockHistories.ToListAsync();
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual(2, remaining[0].HistoryId); // Only the recent record remains
    }

    [Test]
    public async Task DeleteOldApiCallLogsAsync_DeletesOnlyRecordsOlderThan90Days()
    {
        await _autoDeleteAction.DeleteOldApiCallLogsAsync(CancellationToken.None);

        var remaining = await _dbContext.ApiCallLog.ToListAsync();
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual(2, remaining[0].Id); // Only the recent record remains
    }
}
