namespace OcStockAPI.Entities;

[Table("Portfolio_Stocks")]
public class PortfolioStock
{
    public int PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; }
    public int StockId { get; set; }
    public Stock Stock { get; set; }
}
