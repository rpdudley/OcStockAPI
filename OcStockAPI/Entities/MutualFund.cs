namespace OcStockAPI.Entities;

[Table("MutualFunds")]
public class MutualFund
{
    [Key]
    public int MutualFundId { get; set; }
    public string Type { get; set; }
    public decimal Rate { get; set; }
    public ICollection<PortfolioMutualFund> PortfolioMutualFunds { get; set; }
    public ICollection<EventMutualFund> EventMutualFunds { get; set; }
}
