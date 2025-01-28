using System.ComponentModel.DataAnnotations;

namespace KubsConnect.Settings
{
    public class DBConnectionSettings
    {
        [Required]
        public string MySqlDB { get; set; } = String.Empty;
    }
}