using OcStockAPI.Actions;

namespace OcStockAPI.Services
{
    public class DataCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataCleanupBackgroundService> _logger;

        public DataCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DataCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataCleanupBackgroundService started at: {Time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var autoDeleteService = scope.ServiceProvider.GetRequiredService<IAutoDeleteService>();

                        // Call the updated methods with CancellationToken
                        await autoDeleteService.DeleteOldStockHistoryAsync(stoppingToken);
                        await autoDeleteService.DeleteOldApiCallLogsAsync(stoppingToken);

                        _logger.LogInformation("Data cleanup completed at: {Time}", DateTime.UtcNow);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Data cleanup operation was cancelled.");

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during data cleanup.");
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }

            _logger.LogInformation("DataCleanupBackgroundService is stopping at: {Time}", DateTime.UtcNow);
        }
    }
}
