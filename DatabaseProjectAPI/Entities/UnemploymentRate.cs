using System.Text.Json.Serialization;

namespace DatabaseProjectAPI.Entities
{
    public class UnemploymentRate
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("interval")]
        public string Interval { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("data")]
        public List<UnemploymentRateDataPoint> Data { get; set; }
    }

    public class UnemploymentRateDataPoint
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
