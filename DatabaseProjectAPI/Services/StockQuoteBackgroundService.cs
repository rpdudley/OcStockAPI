using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Helpers;

namespace DatabaseProjectAPI.Services
{
    public class StockQuoteBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockQuoteBackgroundService> _logger;

        public StockQuoteBackgroundService(IServiceProvider serviceProvider, ILogger<StockQuoteBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StockQuoteBackgroundService started at: {time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DpapiDbContext>();
                    var apiRequestLogger = scope.ServiceProvider.GetRequiredService<IApiRequestLogger>();

                    // Check if API call has already been made today for market open or close
                    if (IsMarketOpenTime(now) && !await apiRequestLogger.HasMadeApiCallToday("MarketOpen", "AAPL"))
                    {
                        await FetchAndSaveStockData(dbContext, apiRequestLogger, "MarketOpen", "AAPL");
                    }
                    else if (IsMarketCloseTime(now) && !await apiRequestLogger.HasMadeApiCallToday("MarketClose", "AAPL"))
                    {
                        await FetchAndSaveStockData(dbContext, apiRequestLogger, "MarketClose", "AAPL");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);  // Delay for 1 hour
            }
        }
        private async Task FetchAndSaveStockData(DpapiDbContext dbContext, IApiRequestLogger apiRequestLogger, string callType, string symbol)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var stockService = scope.ServiceProvider.GetRequiredService<IAlphaVantageService>();

                try
                {
                    var stockQuote = await stockService.GetStockQuote(symbol);

                    // Find stock by symbol
                    var stock = await dbContext.Stocks.FirstOrDefaultAsync(s => s.Symbol == stockQuote.Symbol);

                    if (stock == null)
                    {
                        _logger.LogWarning("Stock not found in the database. Symbol: {Symbol}", stockQuote.Symbol);
                        return;
                    }

                    var stockHistory = new StockHistory
                    {
                        StockId = stock.StockId,
                        Timestamp = stockQuote.LatestTradingDay,
                        OpenedValue = stockQuote.Open,
                        ClosedValue = stockQuote.Price
                    };

                    dbContext.StockHistories.Add(stockHistory);
                    await dbContext.SaveChangesAsync();

                    // Log that an API call was made
                    await apiRequestLogger.LogApiCall(callType, symbol);

                    _logger.LogInformation("Stock history saved successfully for {Symbol} at {Timestamp}", stock.Symbol, stockHistory.Timestamp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching and saving stock history data.");
                }
            }
        }
    

        private bool IsMarketOpenTime(DateTime currentTime)
        {
            TimeZoneInfo easternTime = GetEasternTimeZone();
            var marketOpenTime = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 9, 30, 0), easternTime);
            return currentTime >= marketOpenTime && currentTime < marketOpenTime.AddMinutes(1); // Within 1 minute of market opening
        }

        private bool IsMarketCloseTime(DateTime currentTime)
        {
            TimeZoneInfo easternTime = GetEasternTimeZone();
            var marketCloseTime = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 16, 0, 0), easternTime);
            return currentTime >= marketCloseTime && currentTime < marketCloseTime.AddMinutes(1); // Within 1 minute of market closing
        }
        private TimeZoneInfo GetEasternTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
            catch (InvalidTimeZoneException)
            {
                throw new Exception("Unable to determine the Eastern Time zone.");
            }
        }

    }

}
