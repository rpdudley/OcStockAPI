namespace XUnitTests.ServiceTests;

public class DataCleanupBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CallsAutoDeleteMethodsOnce()
    {
        // Arrange
        var autoDeleteServiceMock = new Mock<IAutoDeleteService>();
        autoDeleteServiceMock.Setup(x => x.DeleteOldStockHistoryAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        autoDeleteServiceMock.Setup(x => x.DeleteOldApiCallLogsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var scopedServiceProviderMock = new Mock<IServiceProvider>();
        scopedServiceProviderMock.Setup(x => x.GetService(typeof(IAutoDeleteService))).Returns(autoDeleteServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedServiceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var rootServiceProviderMock = new Mock<IServiceProvider>();
        rootServiceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var loggerMock = new Mock<ILogger<DataCleanupBackgroundService>>();

        var service = new DataCleanupBackgroundService(rootServiceProviderMock.Object, loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // short delay

        // Act
        await service.StartAsync(cts.Token);

        // Assert
        autoDeleteServiceMock.Verify(x => x.DeleteOldStockHistoryAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        autoDeleteServiceMock.Verify(x => x.DeleteOldApiCallLogsAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
