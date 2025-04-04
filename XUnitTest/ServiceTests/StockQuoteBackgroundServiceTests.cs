using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Helpers;
using DatabaseProjectAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace XUnitTests.ServiceTests
{
    public class StockQuoteBackgroundServiceTests
    {
        private readonly Mock<ILogger<StockQuoteBackgroundService>> _loggerMock = new();
        private readonly Mock<IFinnhubService> _finnhubServiceMock = new();
        private readonly Mock<IAlphaVantageService> _alphaVantageServiceMock = new();
        private readonly Mock<IApiRequestLogger> _apiRequestLoggerMock = new();
        private readonly Mock<IAutoDeleteService> _autoDeleteServiceMock = new();
        private readonly DbContextOptions<DpapiDbContext> _dbContextOptions;

        public StockQuoteBackgroundServiceTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<DpapiDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task ExecuteAsync_FetchesAndSavesStockData_WhenMarketIsClosed()
        {
            // Arrange
            var dbContext = new DpapiDbContext(_dbContextOptions);
            var trackedStock = new TrackedStock { Id = 1, Symbol = "AAPL", StockName = "Apple Inc." };
            dbContext.TrackedStocks.Add(trackedStock);
            await dbContext.SaveChangesAsync();

            _finnhubServiceMock.Setup(s => s.MarkStatusAsync()).ReturnsAsync(new FinnhubMarketStatus { isOpen = false });
            _apiRequestLoggerMock.Setup(s => s.HasMadeApiCallTodayAsync("MarketClose", trackedStock.Symbol, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _alphaVantageServiceMock.Setup(s => s.GetStockQuoteAsync(trackedStock.Symbol)).ReturnsAsync(new StockQuote
            {
                Symbol = trackedStock.Symbol,
                Open = 150,
                Price = 155,
                Volume = 100000,
                LatestTradingDay = DateTime.UtcNow.Date
            });

            var services = new ServiceCollection();
            services.AddSingleton(_ => dbContext);
            services.AddSingleton(_apiRequestLoggerMock.Object);
            services.AddSingleton(_autoDeleteServiceMock.Object);
            services.AddSingleton(_alphaVantageServiceMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var backgroundService = new StockQuoteBackgroundService(serviceProvider, _loggerMock.Object, _finnhubServiceMock.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Act
            var runTask = backgroundService.StartAsync(cts.Token);
            await Task.Delay(3000); // simulate execution time
            cts.Cancel();
            await runTask;

            // Assert
            var savedStock = await dbContext.Stocks.FirstOrDefaultAsync();
            Assert.NotNull(savedStock);
            Assert.Equal(150, savedStock.OpenValue);
            Assert.Equal(155, savedStock.ClosingValue);
            var history = await dbContext.StockHistories.FirstOrDefaultAsync();
            Assert.NotNull(history);
            Assert.Equal(150, history.OpenedValue);
        }
    }
}