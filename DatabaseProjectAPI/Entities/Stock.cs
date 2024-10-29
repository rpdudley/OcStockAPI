namespace DatabaseProjectAPI.Entities
{
    [Table("Stocks")]
    public class Stock
    {
        [Key]
        [Column("stock_id")]
        public int StockId { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
        public decimal OpenValue { get; set; }
        public decimal ClosingValue { get; set; }

        [Column("tracked_stock_id")]
        public int TrackedStockId { get; set; } // Foreign Key to TrackedStock

        public long? Volume { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation Properties
        public TrackedStock? TrackedStock { get; set; }
        public ICollection<PortfolioStock>? PortfolioStocks { get; set; }
        public ICollection<StockHistory>? StockHistories { get; set; }
        public ICollection<EventStock>? EventStocks { get; set; }
        public ICollection<MarketNews>? MarketNews { get; set; }
    }
}
