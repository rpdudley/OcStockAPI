namespace MSTests.ServicesTests;

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
        // Setup in-memory DbContext
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new DpapiDbContext(options);

        // Create mocks
        _alphaServiceMock = new Mock<IAlphaVantageService>();
        _loggerMock = new Mock<ILogger<EventsBackgroundService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        // Set up AlphaVantageService fake return values
        _alphaServiceMock.Setup(x => x.GetInflationAsync())
            .ReturnsAsync(new Inflation
            {
                Data = new List<InflationDataPoint>
                {
            new InflationDataPoint { Date = "2025-03-01", Value = "3.2" }
                }
            });

        _alphaServiceMock.Setup(x => x.GetFederalInterestRateAsync())
            .ReturnsAsync(new FederalInterestRate
            {
                Data = new List<FederalInterestRateDataPoint>
                {
            new FederalInterestRateDataPoint { Date = "2025-03-01", Value = "5.1" }
                }
            });

        _alphaServiceMock.Setup(x => x.GetUnemploymentRateAsync())
            .ReturnsAsync(new UnemploymentRate
            {
                Data = new List<UnemploymentRateDataPoint>
                {
            new UnemploymentRateDataPoint { Date = "2025-03-01", Value = "3.7" }
                }
            });

        _alphaServiceMock.Setup(x => x.GetCPIdataAsync())
            .ReturnsAsync(new CPIdata
            {
                Data = new List<CpiDataPoint>
                {
            new CpiDataPoint { Date = "2025-03-01", Value = "299.5" }
                }
            });

        // Setup mock service scope
        _serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(DpapiDbContext)))
            .Returns(_dbContext);

        _serviceScopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IAlphaVantageService)))
            .Returns(_alphaServiceMock.Object);

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);

        // Setup service provider to return the scope factory
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_scopeFactoryMock.Object);

        // Construct the background service
        _service = new EventsBackgroundService(_serviceProviderMock.Object, _loggerMock.Object);
    }


    [TestMethod]
    public async Task ExecuteAsync_PerformsDataFetchAndSave()
    {
        using var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await _service.StartAsync(tokenSource.Token);

        var savedEvent = _dbContext.Events.FirstOrDefault();
        Assert.IsNotNull(savedEvent);
        Assert.AreEqual(3.2m, savedEvent.Inflation);
        Assert.AreEqual(5.1m, savedEvent.FederalInterestRate);
        Assert.AreEqual(3.7m, savedEvent.UnemploymentRate);
        Assert.AreEqual(299.5m, savedEvent.CPI);
    }
}
