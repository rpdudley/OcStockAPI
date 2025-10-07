using OcStockAPI.DataContext;
using OcStockAPI.Entities;
using System.Collections.Generic;
using System.Linq;

namespace OcStockAPI.Actions
{
    public interface IStockHistoryAction
    {
        List<StockHistory> GetStockHistory(string symbol);
        List<StockHistory> GetStockHistory(string symbol, DateTime fromDate, DateTime toDate);
    }

    public class StockHistoryAction : IStockHistoryAction
    {
        private readonly OcStockDbContext _OcStockDbContext;

        public StockHistoryAction(OcStockDbContext OcStockDbContext)
        {
            _OcStockDbContext = OcStockDbContext;
        }

        public List<StockHistory> GetStockHistory(string symbol)
        {
            return _OcStockDbContext.StockHistories
                .Where(sh => sh.Stock.Symbol == symbol)  
                .OrderByDescending(sh => sh.Timestamp)  
                .ToList();
        }

        public List<StockHistory> GetStockHistory(string symbol, DateTime fromDate, DateTime toDate)
        {
            return _OcStockDbContext.StockHistories
                .Where(sh => sh.Stock.Symbol == symbol &&
                             sh.Timestamp >= fromDate &&
                             sh.Timestamp <= toDate) 
                .OrderByDescending(sh => sh.Timestamp)
                .ToList();
        }
    }
}
