using System.ComponentModel.DataAnnotations;

namespace OcStockAPI.DTOs
{
    public class TrackedStockDto
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string? StockName { get; set; }
        public DateTime? DateAdded { get; set; }
    }

    public class AddTrackedStockRequest
    {
        [Required]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "Stock symbol must be between 1 and 10 characters")]
        public string Symbol { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Stock name cannot exceed 100 characters")]
        public string? StockName { get; set; }
    }

    public class UpdateTrackedStockRequest
    {
        [StringLength(100, ErrorMessage = "Stock name cannot exceed 100 characters")]
        public string? StockName { get; set; }
    }

    public class TrackedStockResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TrackedStockDto? Data { get; set; }
        public List<TrackedStockDto>? TrackedStocks { get; set; }
    }
}