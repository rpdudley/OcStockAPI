namespace XUnitTests;
public class EventsBackgroundServiceTests
{
    private readonly DpapiDbContext _dbContext;
    private readonly EventsBackgroundService _service;
    private readonly Mock<IAlphaVantageService> _alphaVantageServiceMock;
    private readonly Mock<ILogger<EventsBackgroundService>> _loggerMock;

    public EventsBackgroundServiceTests()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _alphaVantageServiceMock = new Mock<IAlphaVantageService>();
        _loggerMock = new Mock<ILogger<EventsBackgroundService>>();

        // ServiceProvider inside scope
        var scopedProviderMock = new Mock<IServiceProvider>();
        scopedProviderMock.Setup(x => x.GetService(typeof(DpapiDbContext))).Returns(_dbContext);
        scopedProviderMock.Setup(x => x.GetService(typeof(IAlphaVantageService))).Returns(_alphaVantageServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var rootProviderMock = new Mock<IServiceProvider>();
        rootProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        _service = new EventsBackgroundService(rootProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CallsAllFetchMethods()
    {
        // Arrange
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        _alphaVantageServiceMock.Setup(a => a.GetInflationAsync()).ReturnsAsync(
            new Inflation { Data = new List<InflationDataPoint> { new() { Date = today, Value = "3.5" } } });
        _alphaVantageServiceMock.Setup(a => a.GetFederalInterestRateAsync()).ReturnsAsync(
            new FederalInterestRate { Data = new List<FederalInterestRateDataPoint> { new() { Date = today, Value = "4.5" } } });
        _alphaVantageServiceMock.Setup(a => a.GetUnemploymentRateAsync()).ReturnsAsync(
            new UnemploymentRate { Data = new List<UnemploymentRateDataPoint> { new() { Date = today, Value = "5.1" } } });
        _alphaVantageServiceMock.Setup(a => a.GetCPIdataAsync()).ReturnsAsync(
            new CPIdata { Data = new List<CpiDataPoint> { new() { Date = today, Value = "260.7" } } });

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        await _service.StartAsync(cts.Token);

        // Assert
        var @event = await _dbContext.Events.FirstOrDefaultAsync();
        Assert.NotNull(@event);
        Assert.Equal(3.5m, @event.Inflation);
        Assert.Equal(4.5m, @event.FederalInterestRate);
        Assert.Equal(5.1m, @event.UnemploymentRate);
        Assert.Equal(260.7m, @event.CPI);
    }
}