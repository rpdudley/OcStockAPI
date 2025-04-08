using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;

namespace DatabaseProjectAPI.Actions
{
    public interface IMarketNewsAction
    {
        Task<List<MarketNews>> GetMarketNewsByDateAndStockId(DateTime date, int stockId);
    }

    public class MarketNewsAction : IMarketNewsAction
    {
        private readonly DpapiDbContext _dbContext;

        public MarketNewsAction(DpapiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MarketNews>> GetMarketNewsByDateAndStockId(DateTime date, int stockId)
        {
            return await _dbContext.MarketNews
                .Where(mn => mn.StockId == stockId && mn.Datetime.Date == date.Date)
                .OrderByDescending(mn => mn.Datetime)
                .ToListAsync();
        }
    }
}
