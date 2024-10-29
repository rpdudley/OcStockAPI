using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace DatabaseProjectAPI.Helpers
{
    public interface IApiRequestLogger
    {
        Task<bool> HasMadeApiCallToday(string callType, string symbol);
        Task LogApiCall(string callType, string symbol);
    }

    public class ApiRequestLogger : IApiRequestLogger
    {
        private readonly DpapiDbContext _dbContext;
        private readonly ILogger<ApiRequestLogger> _logger;

        public ApiRequestLogger(DpapiDbContext dbContext, ILogger<ApiRequestLogger> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> HasMadeApiCallToday(string callType, string symbol)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbContext.ApiCallLog
                .AnyAsync(log => log.CallDate == today && log.CallType == callType && log.Symbol == symbol);
        }

        public async Task LogApiCall(string callType, string symbol)
        {
            _logger.LogInformation("Logging API call for {Symbol} at {CallType}", symbol, callType);

            var logEntry = new ApiCallLog
            {
                CallDate = DateTime.UtcNow.Date,
                CallType = callType,
                Symbol = symbol
            };

            _dbContext.ApiCallLog.Add(logEntry);
            await _dbContext.SaveChangesAsync();
        }
    }
}
