using System.Text.Json.Serialization;

namespace DatabaseProjectAPI.Entities
{
    public class FederalInterestRate
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("interval")]
        public string Interval { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("data")]
        public List<FederalInterestRateDataPoint> Data { get; set; }
    }

    public class FederalInterestRateDataPoint
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
