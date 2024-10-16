namespace DatabaseProjectAPI.Entities;

[Table("Events")]
public class Event
{
    [Key]
    public int EventId { get; set; }
    public DateTime Datetime { get; set; }
    public decimal ExpectedEps { get; set; }
    public decimal ActualEps { get; set; }
    public decimal InterestRate { get; set; }
    public ICollection<EventStock> EventStocks { get; set; }
    public ICollection<EventMutualFund> EventMutualFunds { get; set; }
}