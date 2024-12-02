using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;

namespace DatabaseProjectAPI.Actions
{
    public interface IStockAction
    {
        Task<Stock> GetStocksById(int id);
        Task<List<Stock>> GetAllStocks();
        Task<List<Stock>> GetStocksBySymbol(string symbol);
    }

    public class StockAction : IStockAction
    {
        private readonly DpapiDbContext _dpapiDbContext;

        public StockAction(DpapiDbContext dpapiDbContext)
        {
            _dpapiDbContext = dpapiDbContext;
        }

        public async Task<Stock> GetStocksById(int id)
        {
            return await _dpapiDbContext.Stocks.FirstOrDefaultAsync(b => b.TrackedStockId == id);
        }

        public async Task<List<Stock>> GetAllStocks()
        {
            return await _dpapiDbContext.Stocks.ToListAsync();
        }

        public async Task<List<Stock>> GetStocksBySymbol(string symbol)
        {
            return await _dpapiDbContext.Stocks
                .Where(s => s.Symbol == symbol)
                .ToListAsync();
        }
    }
}


