namespace DatabaseProjectAPI.Entities;

[Table("Event_MutualFunds")]
public class EventMutualFund
{
    public int EventId { get; set; }
    public Event Event { get; set; }
    public int MutualFundId { get; set; }
    public MutualFund MutualFund { get; set; }
}