using System.Text.Json.Serialization;

namespace OcStockAPI.Helpers
{
    public class AlphaVantageErrorResponse
    {
        [JsonPropertyName("Note")]
        public string? Note { get; set; }
    }
    
    public class AlphaVantageErrorMessage
    {
        [JsonPropertyName("Error Message")]
        public string? ErrorMessage { get; set; }
    }
}
