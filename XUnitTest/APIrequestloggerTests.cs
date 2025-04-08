namespace XUnitTests;
public class ApiRequestLoggerTests
{
    private readonly DpapiDbContext _dbContext;
    private readonly Mock<ILogger<ApiRequestLogger>> _loggerMock;
    private readonly ApiRequestLogger _logger;

    public ApiRequestLoggerTests()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _loggerMock = new Mock<ILogger<ApiRequestLogger>>();
        _logger = new ApiRequestLogger(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task HasMadeApiCallTodayAsync_ReturnsTrue_WhenLogExists()
    {
        var today = DateTime.UtcNow.Date;
        await _dbContext.ApiCallLog.AddAsync(new ApiCallLog
        {
            CallDate = today,
            CallType = "Quote",
            Symbol = "AAPL"
        });
        await _dbContext.SaveChangesAsync();

        var result = await _logger.HasMadeApiCallTodayAsync("Quote", "AAPL", CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task HasMadeApiCallTodayAsync_ReturnsFalse_WhenNoLogExists()
    {
        var result = await _logger.HasMadeApiCallTodayAsync("Quote", "MSFT", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task LogApiCallAsync_AddsLogSuccessfully()
    {
        var symbol = "GOOG";
        var callType = "News";

        await _logger.LogApiCallAsync(callType, symbol, CancellationToken.None);

        var entry = await _dbContext.ApiCallLog.FirstOrDefaultAsync(a => a.Symbol == symbol && a.CallType == callType);
        Assert.NotNull(entry);
        Assert.Equal(DateTime.UtcNow.Date, entry.CallDate);
    }
}
