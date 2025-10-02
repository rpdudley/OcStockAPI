using System.Text.Json.Serialization;

namespace OcStockAPI.Entities;

public class AlphaVantageResponse
{
    [JsonPropertyName("Global Quote")]
    public GlobalQuote? GlobalQuote { get; set; }
}

//This is what is mapping out the json properties this is set by the alpha vantage api, you can add any poperties to the action in services folder to get more resuilts back 
public class GlobalQuote
{
    [JsonPropertyName("01. symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("02. open")]
    public decimal Open { get; set; }

    [JsonPropertyName("03. high")]
    public decimal High { get; set; }

    [JsonPropertyName("04. low")]
    public decimal Low { get; set; }

    [JsonPropertyName("05. price")]
    public decimal Price { get; set; }

    [JsonPropertyName("06. volume")]
    public long Volume { get; set; }

    [JsonPropertyName("07. latest trading day")]
    public DateTime LatestTradingDay { get; set; }

    [JsonPropertyName("08. previous close")]
    public decimal PreviousClose { get; set; }

    [JsonPropertyName("10. change percent")]
    public string ChangePercent { get; set; } = string.Empty;
}
