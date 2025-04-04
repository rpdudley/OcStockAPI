using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace XUnitTests.ServiceTests;

public class EventsBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<EventsBackgroundService>> _loggerMock;
    private readonly Mock<IAlphaVantageService> _alphaVantageServiceMock;
    private readonly DpapiDbContext _dbContext;
    private readonly EventsBackgroundService _service;

    public EventsBackgroundServiceTests()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);
        _loggerMock = new Mock<ILogger<EventsBackgroundService>>();
        _alphaVantageServiceMock = new Mock<IAlphaVantageService>();

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();

        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_scopeFactoryMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(DpapiDbContext)))
            .Returns(_dbContext);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IAlphaVantageService)))
            .Returns(_alphaVantageServiceMock.Object);

        _service = new EventsBackgroundService(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CallsAllFetchMethods()
    {
        // Arrange mock data
        var now = DateTime.UtcNow;
        _alphaVantageServiceMock.Setup(a => a.GetInflationAsync()).ReturnsAsync(
            new Inflation { Data = new List<InflationDataPoint> { new InflationDataPoint { Date = now.ToString("yyyy-MM-dd"), Value = "3.5" } } });
        _alphaVantageServiceMock.Setup(a => a.GetFederalInterestRateAsync()).ReturnsAsync(
            new FederalInterestRate { Data = new List<FederalInterestRateDataPoint> { new FederalInterestRateDataPoint { Date = now.ToString("yyyy-MM-dd"), Value = "4.5" } } });
        _alphaVantageServiceMock.Setup(a => a.GetUnemploymentRateAsync()).ReturnsAsync(
            new UnemploymentRate { Data = new List<UnemploymentRateDataPoint> { new UnemploymentRateDataPoint { Date = now.ToString("yyyy-MM-dd"), Value = "5.1" } } });
        _alphaVantageServiceMock.Setup(a => a.GetCPIdataAsync()).ReturnsAsync(
            new CPIdata { Data = new List<CpiDataPoint> { new CpiDataPoint { Date = now.ToString("yyyy-MM-dd"), Value = "260.7" } } });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(200); // run briefly

        // Act
        await _service.StartAsync(cancellationTokenSource.Token);

        // Assert
        Assert.True(await _dbContext.Events.AnyAsync());
    }
}
