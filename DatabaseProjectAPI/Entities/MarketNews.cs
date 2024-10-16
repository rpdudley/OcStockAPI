namespace DatabaseProjectAPI.Entities;

[Table("MarketNews")]
public class MarketNews
{
    [Key]
    public int NewsId { get; set; }
    public int StockId { get; set; }
    public string Headline { get; set; }
    public string SourceUrl { get; set; }
    public DateTime Datetime { get; set; }

    [ForeignKey("StockId")]
    public Stock Stock { get; set; }
}