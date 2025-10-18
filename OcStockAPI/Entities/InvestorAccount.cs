namespace OcStockAPI.Entities;

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
    
    // Foreign key to ApplicationUser
    [Column("user_id")]
    public int? UserId { get; set; }
    
    // Navigation property
    public virtual ApplicationUser? User { get; set; }
    
    public ICollection<Portfolio> Portfolios { get; set; }
}
