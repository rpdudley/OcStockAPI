namespace NUnitTests;

[TestFixture]
public class DataCleanupBackgroundServiceTests
{
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<ILogger<DataCleanupBackgroundService>> _loggerMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private Mock<IAutoDeleteService> _autoDeleteServiceMock;

    [SetUp]
    public void Setup()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<DataCleanupBackgroundService>>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _autoDeleteServiceMock = new Mock<IAutoDeleteService>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                             .Returns(_serviceScopeFactoryMock.Object);

        _serviceScopeFactoryMock.Setup(f => f.CreateScope())
                                 .Returns(_serviceScopeMock.Object);

        _serviceScopeMock.Setup(x => x.ServiceProvider)
                         .Returns(new ServiceCollection()
                                 .AddSingleton(_autoDeleteServiceMock.Object)
                                 .BuildServiceProvider());
    }

    [Test]
    public async Task ExecuteAsync_PerformsCleanupAndLogs()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel shortly to simulate early shutdown

        var service = new DataCleanupBackgroundService(_serviceProviderMock.Object, _loggerMock.Object);

        // Act
        await service.StartAsync(cts.Token);

        // Assert
        _autoDeleteServiceMock.Verify(m => m.DeleteOldStockHistoryAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _autoDeleteServiceMock.Verify(m => m.DeleteOldApiCallLogsAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}