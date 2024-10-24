using DatabaseProjectAPI.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DatabaseProjectAPI.Actions;


public interface IAutoDeleteService
{
    Task DeleteOldStockHistory();
    Task DeleteOldApiCallLogs();
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

    public async Task DeleteOldStockHistory()
    {
        var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

        var oldStockHistories = await _dbContext.StockHistories
            .Where(sh => sh.Timestamp < ninetyDaysAgo)
            .ToListAsync();

        if (oldStockHistories.Any())
        {
            _dbContext.StockHistories.RemoveRange(oldStockHistories);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{Count} old stock history records deleted.", oldStockHistories.Count);
        }
        else
        {
            _logger.LogInformation("No stock history records found to delete.");
        }
    }

    public async Task DeleteOldApiCallLogs()
    {
        var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

        var oldApiCallLogs = await _dbContext.ApiCallLog
            .Where(log => log.CallDate < ninetyDaysAgo)
            .ToListAsync();

        if (oldApiCallLogs.Any())
        {
            _dbContext.ApiCallLog.RemoveRange(oldApiCallLogs);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("{Count} old API call log records deleted.", oldApiCallLogs.Count);
        }
        else
        {
            _logger.LogInformation("No API call log records found to delete.");
        }
    }
}