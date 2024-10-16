namespace DatabaseProjectAPI.Entities;

public class FinnhubMarketStatus
{
    public string exchange { get; set; }
    public object holiday { get; set; }
    public bool isOpen { get; set; }
    public object session { get; set; }
    public int t { get; set; }
    public string timezone { get; set; }
}
