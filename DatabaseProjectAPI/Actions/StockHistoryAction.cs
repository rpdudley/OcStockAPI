using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseProjectAPI.Actions
{
    public interface IStockHistoryAction
    {
        List<StockHistory> GetStockHistory(string symbol);  // Fetch stock history by symbol
    }

    public class StockHistoryAction : IStockHistoryAction
    {
        private readonly DpapiDbContext _dpapiDbContext;

        public StockHistoryAction(DpapiDbContext dpapiDbContext)
        {
            _dpapiDbContext = dpapiDbContext;
        }

        // Retrieve stock history by symbol
        public List<StockHistory> GetStockHistory(string symbol)
        {
            return _dpapiDbContext.StockHistories
                .Where(sh => sh.Stock.Symbol == symbol)  
                .OrderByDescending(sh => sh.Timestamp)  
                .ToList();
        }
    }
}
