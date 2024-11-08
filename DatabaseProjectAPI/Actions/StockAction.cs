using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DatabaseProjectAPI.Actions
{
    public interface IStockAction
    {
        Task<Stock> GetStocksById(int id);
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
    }
}

