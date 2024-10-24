using DatabaseProjectAPI.Actions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DatabaseProjectAPI.Services
{
    public class DataCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataCleanupBackgroundService> _logger;

        public DataCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<DataCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataCleanupBackgroundService started at: {time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var autoDeleteService = scope.ServiceProvider.GetRequiredService<IAutoDeleteService>();

                    await autoDeleteService.DeleteOldStockHistory();
                    await autoDeleteService.DeleteOldApiCallLogs();

                    _logger.LogInformation("Data cleanup completed at: {time}", DateTime.UtcNow);
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}