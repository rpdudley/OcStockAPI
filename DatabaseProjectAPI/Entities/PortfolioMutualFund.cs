namespace DatabaseProjectAPI.Entities;

[Table("Portfolio_MutualFunds")]
public class PortfolioMutualFund
{
    public int PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; }
    public int MutualFundId { get; set; }
    public MutualFund MutualFund { get; set; }
}