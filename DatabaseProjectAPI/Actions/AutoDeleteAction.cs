using DatabaseProjectAPI.DataContext;

namespace DatabaseProjectAPI.Actions
{
    public interface IAutoDeleteService
    {
        Task DeleteOldStockHistoryAsync(CancellationToken cancellationToken);
        Task DeleteOldApiCallLogsAsync(CancellationToken cancellationToken);
    }

    public class AutoDeleteAction : IAutoDeleteService
    {
        private readonly DpapiDbContext _dbContext;
        private readonly ILogger<AutoDeleteAction> _logger;

        public AutoDeleteAction(DpapiDbContext dbContext, ILogger<AutoDeleteAction> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task DeleteOldStockHistoryAsync(CancellationToken cancellationToken)
        {
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

            try
            {

                int deletedCount = await _dbContext.StockHistories
                    .Where(sh => sh.Timestamp < ninetyDaysAgo)
                    .ExecuteDeleteAsync(cancellationToken);

                if (deletedCount > 0)
                {
                    _logger.LogInformation("{Count} old stock history records deleted.", deletedCount);
                }
                else
                {
                    _logger.LogInformation("No stock history records found to delete.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Deletion of old stock history was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting old stock history records.");
                throw;
            }
        }

        public async Task DeleteOldApiCallLogsAsync(CancellationToken cancellationToken)
        {
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

            try
            {
                // Use ExecuteDeleteAsync for efficient bulk deletion (EF Core 7.0+)
                int deletedCount = await _dbContext.ApiCallLog
                    .Where(log => log.CallDate < ninetyDaysAgo)
                    .ExecuteDeleteAsync(cancellationToken);

                if (deletedCount > 0)
                {
                    _logger.LogInformation("{Count} old API call log records deleted.", deletedCount);
                }
                else
                {
                    _logger.LogInformation("No API call log records found to delete.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Deletion of old API call logs was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting old API call log records.");
                throw;
            }
        }
    }
}
