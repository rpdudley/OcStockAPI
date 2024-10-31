namespace DatabaseProjectAPI.Entities;

[Table("StockHistory")]
public class StockHistory
{
    [Key]
    public int HistoryId { get; set; }

    [Column("stock_id")]
    public int StockId { get; set; } // Foreign Key to Stock

    public DateTime? Timestamp { get; set; }
    public decimal? OpenedValue { get; set; }
    public decimal? ClosedValue { get; set; }
    public long Volume { get; set; }

    // Navigation Property to Stock
    public Stock Stock { get; set; }
}
