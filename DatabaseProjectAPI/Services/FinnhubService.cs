using DatabaseProjectAPI.Entities;
using KubsConnect.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseProjectAPI.Services
{
    public interface IFinnhubService
    {
        Task<JObject> GetStockDataAsync(string symbol, DateTime fromDate, DateTime toDate);
        Task<FinnhubMarketStatus> MarkStatusAsync();
    }

    public class FinnhubService : IFinnhubService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FinnhubService(HttpClient httpClient, FinnhubSettings settings)
        {
            _httpClient = httpClient;
            _apiKey = settings.ApiKey;
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
        public async Task<FinnhubMarketStatus> MarkStatusAsync()
        {
            string url = $"https://finnhub.io/api/v1/stock/market-status?exchange=US&token={_apiKey}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<FinnhubMarketStatus>(await response.Content.ReadAsStringAsync());
        }
    }
}