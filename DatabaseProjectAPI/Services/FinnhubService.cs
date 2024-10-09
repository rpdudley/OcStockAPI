using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using DatabaseProjectAPI.Entities.Settings;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DatabaseProjectAPI.Services
{
    public interface IFinnhubService
    {
        Task<JObject> GetStockDataAsync(string symbol, DateTime fromDate, DateTime toDate);
        Task<Rootobject> MarkStatusAsync();
    }

    public class FinnhubService : IFinnhubService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FinnhubService(HttpClient httpClient, IOptions<FinnhubSettings> settings)
        {
            _httpClient = httpClient;
            _apiKey = settings.Value.ApiKey;
        }

        public async Task<JObject> GetStockDataAsync(string symbol, DateTime fromDate, DateTime toDate)
        {
            long fromTimestamp = new DateTimeOffset(fromDate).ToUnixTimeSeconds();
            long toTimestamp = new DateTimeOffset(toDate).ToUnixTimeSeconds();
            //string url = $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution=D&from={fromTimestamp}&to={toTimestamp}&token={_apiKey}";
            string url = $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution=D&from={fromTimestamp}&to={toTimestamp}&token={_apiKey}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();


            string jsonResult = await response.Content.ReadAsStringAsync();
            return JObject.Parse(jsonResult);
        }
        public async Task<Rootobject> MarkStatusAsync()
        {
            string url = $"https://finnhub.io/api/v1/stock/market-status?exchange=US&token={_apiKey}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            return JsonConvert.DeserializeObject<Rootobject>(await response.Content.ReadAsStringAsync());
        }
    }
    public class Rootobject
    {
        public string exchange { get; set; }
        public object holiday { get; set; }
        public bool isOpen { get; set; }
        public object session { get; set; }
        public int t { get; set; }
        public string timezone { get; set; }
    }

}