namespace DatabaseProjectAPI.Entities
{
    [Table("StockHistory")]
    public class StockHistory
    {
        [Key]
        [Column("history_id")]
        public int HistoryId { get; set; }
        [Column("stock_id")]
        public int StockId { get; set; }
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }
        [Column("opened_value")]
        public decimal OpenedValue { get; set; }
        [Column("closed_value")]
        public decimal ClosedValue { get; set; }
        [Column("Symbol")]
        public string? Symbol { get; set; }
        [ForeignKey("StockId")]
        public Stock? Stock { get; set; }
    }
}