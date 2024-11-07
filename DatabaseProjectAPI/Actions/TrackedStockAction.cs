using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;

namespace DatabaseProjectAPI.Actions;

public interface ITrackedStockAction
{
    List<TrackedStock> GetTrackedStocks(string symbol= " ");      
    void AddTrackedStock(TrackedStock stock);   
}

public class TrackedStockAction : ITrackedStockAction
{
    private readonly DpapiDbContext _dpapiDbContext;

    public TrackedStockAction(DpapiDbContext dpapiDbContext)
    {
        _dpapiDbContext = dpapiDbContext;
    }
    public List<TrackedStock> GetTrackedStocks(string symbol = "")
    {
        return _dpapiDbContext.TrackedStocks
            .Where(s => string.IsNullOrEmpty(symbol) || s.Symbol.StartsWith(symbol)) 
            .OrderBy(s => s.Id)
            .ToList();
    }
    public void AddTrackedStock(TrackedStock stock)
    {
        if (!_dpapiDbContext.TrackedStocks.Any(s => s.Symbol == stock.Symbol))
        {
            _dpapiDbContext.TrackedStocks.Add(stock);
            _dpapiDbContext.SaveChanges();
        }
    }
}
