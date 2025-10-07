using System.Text.Json.Serialization;

namespace OcStockAPI.Entities;

public class NewsApiResponse
{
    [JsonPropertyName("articles")]
    public List<Article> Articles { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class Article
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }
}
