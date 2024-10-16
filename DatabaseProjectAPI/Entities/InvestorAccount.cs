namespace DatabaseProjectAPI.Entities;

[Table("InvestorAccount")]
public class InvestorAccount
{
    [Key]
    [Column("account_ID")]
    public int AccountId { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [Column("name")]
    public string Name { get; set; }
    public ICollection<Portfolio> Portfolios { get; set; }
}