namespace DatabaseProjectAPI.Entities;

[Table("TrackedStocks")]
public class TrackedStock
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string? Symbol { get; set; }
    public string? StockName { get; set; }

    // Navigation Property to Stocks
    public ICollection<Stock> Stocks { get; set; }
}
