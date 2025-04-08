namespace MSTests;

[TestClass]
public class EventsBackgroundServiceTests
{
    private EventsBackgroundService _service;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IAlphaVantageService> _alphaServiceMock;
    private Mock<ILogger<EventsBackgroundService>> _loggerMock;
    private DpapiDbContext _dbContext;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        _alphaServiceMock = new Mock<IAlphaVantageService>();
        _loggerMock = new Mock<ILogger<EventsBackgroundService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        string date = "2025-03-01";

        _alphaServiceMock.Setup(x => x.GetInflationAsync())
            .ReturnsAsync(new Inflation
            {
                Data = new List<InflationDataPoint>
                {
                    new() { Date = date, Value = "3.2" }
                }
            });

        _alphaServiceMock.Setup(x => x.GetFederalInterestRateAsync())
            .ReturnsAsync(new FederalInterestRate
            {
                Data = new List<FederalInterestRateDataPoint>
                {
                    new() { Date = date, Value = "5.1" }
                }
            });

        _alphaServiceMock.Setup(x => x.GetUnemploymentRateAsync())
            .ReturnsAsync(new UnemploymentRate
            {
                Data = new List<UnemploymentRateDataPoint>
                {
                    new() { Date = date, Value = "3.7" }
                }
            });

        _alphaServiceMock.Setup(x => x.GetCPIdataAsync())
            .ReturnsAsync(new CPIdata
            {
                Data = new List<CpiDataPoint>
                {
                    new() { Date = date, Value = "299.5" }
                }
            });

        var scopeServiceProvider = new Mock<IServiceProvider>();
        scopeServiceProvider.Setup(sp => sp.GetService(typeof(DpapiDbContext))).Returns(_dbContext);
        scopeServiceProvider.Setup(sp => sp.GetService(typeof(IAlphaVantageService))).Returns(_alphaServiceMock.Object);

        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(scopeServiceProvider.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_scopeFactoryMock.Object);

        _service = new EventsBackgroundService(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_PerformsDataFetchAndSave()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await _service.StartAsync(cts.Token);

        var savedEvent = _dbContext.Events.FirstOrDefault();

        Assert.IsNotNull(savedEvent);
        Assert.AreEqual(3.2m, savedEvent.Inflation);
        Assert.AreEqual(5.1m, savedEvent.FederalInterestRate);
        Assert.AreEqual(3.7m, savedEvent.UnemploymentRate);
        Assert.AreEqual(299.5m, savedEvent.CPI);
    }
}
