using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OcStockAPI.Entities.Identity;

public class ApplicationUser : IdentityUser<int>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<InvestorAccount> InvestorAccounts { get; set; } = new List<InvestorAccount>();
    
    public string FullName => $"{FirstName} {LastName}";
}