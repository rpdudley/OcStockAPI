using OcStockAPI.DataContext;
using OcStockAPI.Entities;

namespace OcStockAPI.Actions
{
    public interface IStockAction
    {
        Task<Stock> GetStocksById(int id);
        Task<List<Stock>> GetAllStocks();
        Task<List<Stock>> GetStocksBySymbol(string symbol);
    }

    public class StockAction : IStockAction
    {
        private readonly OcStockDbContext _OcStockDbContext;

        public StockAction(OcStockDbContext OcStockDbContext)
        {
            _OcStockDbContext = OcStockDbContext;
        }

        public async Task<Stock> GetStocksById(int id)
        {
            return await _OcStockDbContext.Stocks.FirstOrDefaultAsync(b => b.TrackedStockId == id);
        }

        public async Task<List<Stock>> GetAllStocks()
        {
            return await _OcStockDbContext.Stocks.ToListAsync();
        }

        public async Task<List<Stock>> GetStocksBySymbol(string symbol)
        {
            return await _OcStockDbContext.Stocks
                .Where(s => s.Symbol == symbol)
                .ToListAsync();
        }
    }
}


