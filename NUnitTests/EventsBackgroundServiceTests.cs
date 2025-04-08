namespace NUnitTests;
public class EventsBackgroundServiceTests
{
    private DpapiDbContext _dbContext;
    private EventsBackgroundService _service;
    private Mock<IAlphaVantageService> _alphaVantageServiceMock;
    private Mock<ILogger<EventsBackgroundService>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _alphaVantageServiceMock = new Mock<IAlphaVantageService>();
        _loggerMock = new Mock<ILogger<EventsBackgroundService>>();

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

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _service.Dispose();
    }

    [Test]
    public async Task ExecuteAsync_CallsAllFetchMethods()
    {
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

        await _service.StartAsync(cts.Token);

        var @event = await _dbContext.Events.FirstOrDefaultAsync();
        Assert.IsNotNull(@event);
        Assert.AreEqual(3.5m, @event.Inflation);
        Assert.AreEqual(4.5m, @event.FederalInterestRate);
        Assert.AreEqual(5.1m, @event.UnemploymentRate);
        Assert.AreEqual(260.7m, @event.CPI);
    }
}