using Newtonsoft.Json;

namespace DatabaseProjectAPI.Entities;

public class NewsApiResponse
{
    [JsonProperty("articles")]
    public List<Article> Articles { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}

public class Article
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("author")]
    public string Author { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }
}