using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                if (IsMarketOpenTime(now) || IsMarketCloseTime(now))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var stockService = scope.ServiceProvider.GetRequiredService<IAlphaVantageService>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<DpapiDbContext>();

                        try
                        {
                            var stockQuote = await stockService.GetStockQuote("AAPL"); 

                            var stock = await dbContext.Stocks
                                .FirstOrDefaultAsync(s => s.Symbol == stockQuote.Symbol, stoppingToken);

                            if (stock == null)
                            {
                                _logger.LogWarning("Stock not found in the database. Symbol: {Symbol}", stockQuote.Symbol);
                                continue;
                            }

                            var stockHistory = new StockHistory
                            {
                                StockId = stock.StockId,
                                Timestamp = stockQuote.LatestTradingDay,  
                                OpenedValue = stockQuote.Open,           
                                ClosedValue = stockQuote.Price           
                            };

                            
                            dbContext.StockHistories.Add(stockHistory);
                            await dbContext.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Stock history saved successfully for {Symbol} at {Timestamp}", stock.Symbol, stockHistory.Timestamp);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error occurred while fetching and saving stock history data.");
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken); // Check every hour
            }
        }

        private bool IsMarketOpenTime(DateTime currentTime)
        {
            TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var marketOpenTime = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 9, 30, 0), est);
            return currentTime >= marketOpenTime && currentTime < marketOpenTime.AddMinutes(1); // Within 1 minute of market opening
        }

        private bool IsMarketCloseTime(DateTime currentTime)
        {
            TimeZoneInfo est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var marketCloseTime = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 16, 0, 0), est);
            return currentTime >= marketCloseTime && currentTime < marketCloseTime.AddMinutes(1); // Within 1 minute of market closing
        }
    }
}
