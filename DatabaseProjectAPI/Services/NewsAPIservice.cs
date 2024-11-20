using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using KubsConnect.Settings;
using DatabaseProjectAPI.Entities;

namespace DatabaseProjectAPI.Services
{
    public interface INewsAPIService
    {
        Task<List<Article>> GetNewsDataAsync(string name, DateTime from, DateTime to);
    }
        public class NewsAPIService : INewsAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public NewsAPIService(HttpClient httpClient, NewsSettings settings)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RyanStockApp/1.0");
            _apiKey = settings.ApiKey;
        }


        public async Task<List<Article>> GetNewsDataAsync(string name, DateTime from, DateTime to)
        {
            string fromDate = from.ToString("yyyy-MM-dd");
            string toDate = to.ToString("yyyy-MM-dd");

            var url = $"https://newsapi.org/v2/everything?q={name}&from={fromDate}&to={toDate}&sortBy=popularity&apiKey={_apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "RyanStockApp/1.0");

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var articlesResponse = JsonConvert.DeserializeObject<NewsApiResponse>(jsonResponse);
                return articlesResponse?.Articles ?? new List<Article>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request to News API failed with status code: {response.StatusCode}, Content: {errorContent}");
            }
        }
    }
}