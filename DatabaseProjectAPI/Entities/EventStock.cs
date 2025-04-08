using DatabaseProjectAPI.Entities;

[Table("Event_Stocks")]
public class EventStock
{
    [Column("event_id")]
    public int EventId { get; set; }

    [ForeignKey("EventId")]
    public Event Event { get; set; }

    [Column("stock_id")]
    public int StockId { get; set; }

    [ForeignKey("StockId")]
    public Stock Stock { get; set; }
}