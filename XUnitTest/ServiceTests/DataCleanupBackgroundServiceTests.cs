using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace XUnitTests.ServiceTests
{
    public class DataCleanupBackgroundServiceTests
    {
        [Fact]
        public async Task ExecuteAsync_CallsAutoDeleteMethodsOnce()
        {
            // Arrange
            var autoDeleteServiceMock = new Mock<IAutoDeleteService>();
            autoDeleteServiceMock.Setup(x => x.DeleteOldStockHistoryAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            autoDeleteServiceMock.Setup(x => x.DeleteOldApiCallLogsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            var serviceScopeMock = new Mock<IServiceScope>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();

            serviceScopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);

            serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<IAutoDeleteService>()).Returns(autoDeleteServiceMock.Object);

            var loggerMock = new Mock<ILogger<DataCleanupBackgroundService>>();

            var service = new DataCleanupBackgroundService(serviceProviderMock.Object, loggerMock.Object);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1)); // Short-circuit the while loop for test

            // Act
            await service.StartAsync(cts.Token);

            // Assert
            autoDeleteServiceMock.Verify(x => x.DeleteOldStockHistoryAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            autoDeleteServiceMock.Verify(x => x.DeleteOldApiCallLogsAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}
