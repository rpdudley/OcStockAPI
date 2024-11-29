using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseProjectAPI.Actions
{
    public interface IStockHistoryAction
    {
        List<StockHistory> GetStockHistory(string symbol);
        List<StockHistory> GetStockHistory(string symbol, DateTime fromDate, DateTime toDate);
    }

    public class StockHistoryAction : IStockHistoryAction
    {
        private readonly DpapiDbContext _dpapiDbContext;

        public StockHistoryAction(DpapiDbContext dpapiDbContext)
        {
            _dpapiDbContext = dpapiDbContext;
        }

        public List<StockHistory> GetStockHistory(string symbol)
        {
            return _dpapiDbContext.StockHistories
                .Where(sh => sh.Stock.Symbol == symbol)  
                .OrderByDescending(sh => sh.Timestamp)  
                .ToList();
        }

        public List<StockHistory> GetStockHistory(string symbol, DateTime fromDate, DateTime toDate)
        {
            return _dpapiDbContext.StockHistories
                .Where(sh => sh.Stock.Symbol == symbol &&
                             sh.Timestamp >= fromDate &&
                             sh.Timestamp <= toDate) 
                .OrderByDescending(sh => sh.Timestamp)
                .ToList();
        }
    }
}
