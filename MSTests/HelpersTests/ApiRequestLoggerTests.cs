namespace MSTests.HelpersTests;

[TestClass]
public class ApiRequestLoggerTests
{
    private DpapiDbContext _dbContext;
    private Mock<ILogger<ApiRequestLogger>> _loggerMock;
    private ApiRequestLogger _apiRequestLogger;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _loggerMock = new Mock<ILogger<ApiRequestLogger>>();
        _apiRequestLogger = new ApiRequestLogger(_dbContext, _loggerMock.Object);
    }

    [TestMethod]
    public async Task LogApiCallAsync_AddsEntryToDatabase()
    {
        // Arrange
        var callType = "MarketClose";
        var symbol = "AAPL";
        var token = CancellationToken.None;

        // Act
        await _apiRequestLogger.LogApiCallAsync(callType, symbol, token);

        // Assert
        var log = await _dbContext.ApiCallLog.FirstOrDefaultAsync();
        Assert.IsNotNull(log);
        Assert.AreEqual(callType, log.CallType);
        Assert.AreEqual(symbol, log.Symbol);
        Assert.AreEqual(DateTime.UtcNow.Date, log.CallDate);
    }

    [TestMethod]
    public async Task HasMadeApiCallTodayAsync_ReturnsTrue_WhenCallExists()
    {
        // Arrange
        var callType = "MarketClose";
        var symbol = "AAPL";
        var today = DateTime.UtcNow.Date;

        _dbContext.ApiCallLog.Add(new ApiCallLog
        {
            CallDate = today,
            CallType = callType,
            Symbol = symbol
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _apiRequestLogger.HasMadeApiCallTodayAsync(callType, symbol, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HasMadeApiCallTodayAsync_ReturnsFalse_WhenNoCallExists()
    {
        // Arrange
        var callType = "MarketClose";
        var symbol = "MSFT";

        // Act
        var result = await _apiRequestLogger.HasMadeApiCallTodayAsync(callType, symbol, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }
}