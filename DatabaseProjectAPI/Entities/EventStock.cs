namespace DatabaseProjectAPI.Entities;

[Table("Event_Stocks")]
public class EventStock
{
    public int EventId { get; set; }
    public Event Event { get; set; }
    public int StockId { get; set; }
    public Stock Stock { get; set; }
}