using Newtonsoft.Json;

namespace DatabaseProjectAPI.Helpers
{
    public class AlphaVantageErrorResponse
    {
        [JsonProperty("Note")]
        public string? Note { get; set; }
    }
    public class AlphaVantageErrorMessage
    {
        [JsonProperty("Error Message")]
        public string? ErrorMessage { get; set; }
    }
}
