namespace OcStockAPI.Entities.Settings;

[Table("ApiCallLog")]
public class ApiCallLog
{
    [Key]
    public int Id { get; set; }
    [Required]
    public DateTime CallDate { get; set; }
    [Required]
    [MaxLength(50)]
    public string? CallType { get; set; }
    [Required]
    [MaxLength(10)]
    public string? Symbol { get; set; }   
}
