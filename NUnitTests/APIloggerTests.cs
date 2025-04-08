namespace NUnitTests;

public class ApiRequestLoggerTests
{
    private DpapiDbContext _dbContext;
    private Mock<ILogger<ApiRequestLogger>> _loggerMock;
    private ApiRequestLogger _apiRequestLogger;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        _loggerMock = new Mock<ILogger<ApiRequestLogger>>();
        _apiRequestLogger = new ApiRequestLogger(_dbContext, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task LogApiCallAsync_AddsNewEntry()
    {
        var callType = "StockQuote";
        var symbol = "AAPL";
        var token = CancellationToken.None;

        await _apiRequestLogger.LogApiCallAsync(callType, symbol, token);

        var entry = await _dbContext.ApiCallLog
            .FirstOrDefaultAsync(e => e.CallType == callType && e.Symbol == symbol);

        Assert.NotNull(entry);
        Assert.AreEqual(callType, entry.CallType);
        Assert.AreEqual(symbol, entry.Symbol);
        Assert.AreEqual(DateTime.UtcNow.Date, entry.CallDate);
    }

    [Test]
    public async Task HasMadeApiCallTodayAsync_ReturnsTrue_IfLogExists()
    {
        var callType = "News";
        var symbol = "TSLA";

        _dbContext.ApiCallLog.Add(new ApiCallLog
        {
            CallType = callType,
            Symbol = symbol,
            CallDate = DateTime.UtcNow.Date
        });

        await _dbContext.SaveChangesAsync();

        var result = await _apiRequestLogger.HasMadeApiCallTodayAsync(callType, symbol, CancellationToken.None);

        Assert.IsTrue(result);
    }

    [Test]
    public async Task HasMadeApiCallTodayAsync_ReturnsFalse_IfNoLogExists()
    {
        var result = await _apiRequestLogger.HasMadeApiCallTodayAsync("Earnings", "MSFT", CancellationToken.None);
        Assert.IsFalse(result);
    }

    [Test]
    public void HasMadeApiCallTodayAsync_ThrowsOnCanceledToken()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _apiRequestLogger.HasMadeApiCallTodayAsync("Rate", "GOOGL", tokenSource.Token);
        });
    }

    [Test]
    public void LogApiCallAsync_ThrowsOnCanceledToken()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await _apiRequestLogger.LogApiCallAsync("Inflation", "META", tokenSource.Token);
        });
    }
}