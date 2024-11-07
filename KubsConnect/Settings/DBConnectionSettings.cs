using System.ComponentModel.DataAnnotations;

namespace KubsConnect.Settings
{
    public class DBConnectionSettings
    {
        [Required]
        public string RyanWilliamDB { get; set; } = String.Empty;
    }
}
