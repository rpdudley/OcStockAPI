namespace DatabaseProjectAPI.Entities;

[Table("InvestorAccount")]
public class InvestorAccount
{
    [Key]
    public int AccountId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Name { get; set; }
    public ICollection<Portfolio> Portfolios { get; set; }
}