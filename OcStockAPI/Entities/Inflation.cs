using System.Text.Json.Serialization;

namespace OcStockAPI.Entities
{
    public class Inflation
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("interval")]
        public string Interval { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("data")]
        public List<InflationDataPoint> Data { get; set; }
    }

    public class InflationDataPoint
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}

