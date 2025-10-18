using Microsoft.AspNetCore.Identity;

namespace OcStockAPI.Entities.Identity;

public class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}