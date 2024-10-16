namespace DatabaseProjectAPI.Entities
{
    [Table("Portfolio")]
    public class Portfolio
    {
        [Key]
        public int PortfolioId { get; set; }
        public int AccountId { get; set; }
        public string Name { get; set; }

        [ForeignKey("AccountId")]
        public InvestorAccount InvestorAccount { get; set; }
        public ICollection<PortfolioMutualFund> PortfolioMutualFunds { get; set; }
        public ICollection<PortfolioStock> PortfolioStocks { get; set; }
    }
}