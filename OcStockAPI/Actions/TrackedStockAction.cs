using OcStockAPI.DataContext;
using OcStockAPI.Entities;

namespace OcStockAPI.Actions;

public interface ITrackedStockAction
{
    List<TrackedStock> GetTrackedStocks(string symbol= " ");      
    void AddTrackedStock(TrackedStock stock);   
}

public class TrackedStockAction : ITrackedStockAction
{
    private readonly OcStockDbContext _OcStockDbContext;

    public TrackedStockAction(OcStockDbContext OcStockDbContext)
    {
        _OcStockDbContext = OcStockDbContext;
    }
    public List<TrackedStock> GetTrackedStocks(string symbol = "")
    {
        return _OcStockDbContext.TrackedStocks
            .Where(s => string.IsNullOrEmpty(symbol) || s.Symbol.StartsWith(symbol)) 
            .OrderBy(s => s.Id)
            .ToList();
    }
    public void AddTrackedStock(TrackedStock stock)
    {
        if (!_OcStockDbContext.TrackedStocks.Any(s => s.Symbol == stock.Symbol))
        {
            _OcStockDbContext.TrackedStocks.Add(stock);
            _OcStockDbContext.SaveChanges();
        }
    }
}
