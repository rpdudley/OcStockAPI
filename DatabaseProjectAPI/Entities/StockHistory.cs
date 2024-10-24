namespace DatabaseProjectAPI.Entities
{
    [Table("StockHistory")]
    public class StockHistory
    {
        [Key]
        public int HistoryId { get; set; }
        public int StockId { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal OpenedValue { get; set; }
        public decimal ClosedValue { get; set; }
        public string Symbol { get; set; }
        [ForeignKey("StockId")]
        public Stock Stock { get; set; }
    }
}