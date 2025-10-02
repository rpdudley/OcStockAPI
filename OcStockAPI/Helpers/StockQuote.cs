namespace OcStockAPI.Helpers
{
    public class StockQuote
    {
        public string Symbol { get; set; }
        public decimal Open { get; set; }
        public decimal Price { get; set; }
        public long Volume { get; set; }
        public DateTime LatestTradingDay { get; set; }
    }
}
