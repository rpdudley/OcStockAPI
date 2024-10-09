using DatabaseProjectAPI.Entities.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NewsAPI.Constants;
using NewsAPI.Models;
using NewsAPI;

namespace DatabaseProjectAPI.Services;

public interface INewsAPIService
{

}
public class NewsAPIService : INewsAPIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    public NewsAPIService(HttpClient httpClient, IOptions<NewsSettings> settings)
    {
        _httpClient = httpClient;
        _apiKey = settings.Value.ApiKey;
    }
    static void Main(string[] args)
    {
        // init with your API key
        var newsApiClient = new NewsApiClient("e53d610ddd864bb7aab618a7f0e5f0d4");
        var articlesResponse = newsApiClient.GetEverything(new EverythingRequest
        {
            Q = "Apple",
            SortBy = SortBys.Popularity,
            Language = Languages.EN,
            From = new DateTime(2018, 1, 25)
        });
        if (articlesResponse.Status == Statuses.Ok)
        {
            // total results found
            Console.WriteLine(articlesResponse.TotalResults);
            // here's the first 20
            foreach (var article in articlesResponse.Articles)
            {
                // title
                Console.WriteLine(article.Title);
                // author
                Console.WriteLine(article.Author);
                // description
                Console.WriteLine(article.Description);
                // url
                Console.WriteLine(article.Url);
                // published at
                Console.WriteLine(article.PublishedAt);
            }
        }
        Console.ReadLine();
    }
}