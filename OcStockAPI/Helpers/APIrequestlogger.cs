using OcStockAPI.DataContext;
using OcStockAPI.Entities.Settings;

namespace OcStockAPI.Helpers
{
    public interface IApiRequestLogger
    {
        Task<bool> HasMadeApiCallTodayAsync(string callType, string symbol, CancellationToken cancellationToken);
        Task LogApiCallAsync(string callType, string symbol, CancellationToken cancellationToken);
    }

    public class ApiRequestLogger : IApiRequestLogger
    {
        private readonly OcStockDbContext _dbContext;
        private readonly ILogger<ApiRequestLogger> _logger;

        public ApiRequestLogger(OcStockDbContext dbContext, ILogger<ApiRequestLogger> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> HasMadeApiCallTodayAsync(string callType, string symbol, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;

            try
            {
                return await _dbContext.ApiCallLog
                    .AsNoTracking()
                    .AnyAsync(
                        log => log.CallDate == today && log.CallType == callType && log.Symbol == symbol,
                        cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation was cancelled while checking API call logs for {Symbol} and call type {CallType}.", symbol, callType);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking API call logs for {Symbol} and call type {CallType}.", symbol, callType);
                throw;
            }
        }

        public async Task LogApiCallAsync(string callType, string symbol, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Logging API call for {Symbol} with call type {CallType}.", symbol, callType);

            var logEntry = new ApiCallLog
            {
                CallDate = DateTime.UtcNow.Date,
                CallType = callType,
                Symbol = symbol
            };

            try
            {
                await _dbContext.ApiCallLog.AddAsync(logEntry, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("API call logged successfully for {Symbol} with call type {CallType}.", symbol, callType);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation was cancelled while logging API call for {Symbol} and call type {CallType}.", symbol, callType);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging API call for {Symbol} and call type {CallType}.", symbol, callType);
                throw;
            }
        }
    }
}

