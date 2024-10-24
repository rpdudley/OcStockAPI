namespace DatabaseProjectAPI.Entities
{
    [Table("Stocks")]
    public class Stock
    {
        [Key]
        [Column("stock_id")]
        public int StockId { get; set; }
        public string? Symbol { get; set; }
        public decimal OpenValue { get; set; }
        public decimal ClosingValue { get; set; }
        public long Volume { get; set; }
        public ICollection<PortfolioStock> PortfolioStocks { get; set; }
        public ICollection<StockHistory> StockHistories { get; set; }
        public ICollection<EventStock> EventStocks { get; set; }
        public ICollection<MarketNews> MarketNews { get; set; }
    }
}
