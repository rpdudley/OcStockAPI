using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Helpers;
using static DatabaseProjectAPI.Services.AlphaVantageService;

namespace DatabaseProjectAPI.Services
{
    public class StockQuoteBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockQuoteBackgroundService> _logger;
        private readonly IFinnhubService _finnhubService;

        public StockQuoteBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<StockQuoteBackgroundService> logger,
            IFinnhubService finnhubService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _finnhubService = finnhubService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StockQuoteBackgroundService started at: {Time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                var currentTime = DateTime.UtcNow;

                if (currentTime.Hour == 22 && currentTime.Minute == 0 || currentTime.Minute == 1)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<DpapiDbContext>();
                        var apiRequestLogger = scope.ServiceProvider.GetRequiredService<IApiRequestLogger>();
                        var autoDeleteService = scope.ServiceProvider.GetRequiredService<IAutoDeleteService>();

                        // Perform cleanup tasks
                        await autoDeleteService.DeleteOldStockHistoryAsync(stoppingToken);
                        await autoDeleteService.DeleteOldApiCallLogsAsync(stoppingToken);

                        var trackedStocks = await dbContext.TrackedStocks.ToListAsync(stoppingToken);

                        var marketStatus = await _finnhubService.MarkStatusAsync();
                        if (!marketStatus.isOpen)
                        {
                            foreach (var trackedStock in trackedStocks)
                            {
                                if (!await apiRequestLogger.HasMadeApiCallTodayAsync("MarketClose", trackedStock.Symbol, stoppingToken))
                                {
                                    _logger.LogInformation("Market is closed, fetching end-of-day stock data for symbol: {Symbol}", trackedStock.Symbol);
                                    await FetchAndSaveStockDataAsync(dbContext, apiRequestLogger, "MarketClose", trackedStock, stoppingToken);
                                }
                            }
                        }
                    }
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }

                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        private async Task FetchAndSaveStockDataAsync(DpapiDbContext dbContext, IApiRequestLogger apiRequestLogger, string callType, TrackedStock trackedStock, CancellationToken cancellationToken)
        {
            var symbol = trackedStock.Symbol;
            _logger.LogInformation("FetchAndSaveStockDataAsync started for symbol {Symbol} with call type {CallType}", symbol, callType);

            try
            {
                //Resolve the AlphaVantageService within the existing scope
                var stockService = _serviceProvider.GetRequiredService<IAlphaVantageService>();

                //Fetch data from the API
                var stockQuote = await stockService.GetStockQuoteAsync(symbol);

                //Check if there's an existing entry in `Stocks` for this tracked stock
                var stock = await dbContext.Stocks
                    .FirstOrDefaultAsync(s => s.TrackedStockId == trackedStock.Id, cancellationToken);

                if (stock == null)
                {
                    stock = new Stock
                    {
                        TrackedStockId = trackedStock.Id,
                        Name = trackedStock.StockName,
                        Symbol = stockQuote.Symbol,
                        OpenValue = stockQuote.Open,
                        ClosingValue = stockQuote.Price,
                        Volume = stockQuote.Volume,
                        LastUpdated = DateTime.UtcNow
                    };
                    dbContext.Stocks.Add(stock);

                    // Save changes to get the generated StockId
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // Update existing stock entry
                    stock.OpenValue = stockQuote.Open;
                    stock.ClosingValue = stockQuote.Price;
                    stock.Volume = stockQuote.Volume;
                    stock.LastUpdated = DateTime.UtcNow;

                    // Save changes to update the stock
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                // Create a new entry in `StockHistory` to track the history
                var stockHistory = new StockHistory
                {
                    Timestamp = stockQuote.LatestTradingDay,
                    OpenedValue = stockQuote.Open,
                    ClosedValue = stockQuote.Price,
                    Volume = stockQuote.Volume,
                    Stock = stock 
                };

                dbContext.StockHistories.Add(stockHistory);

                await dbContext.SaveChangesAsync(cancellationToken);

                
                await apiRequestLogger.LogApiCallAsync(callType, symbol, cancellationToken);
                _logger.LogInformation("Stock and history data saved successfully for {Symbol} at {Timestamp}", symbol, DateTime.UtcNow);
            }
            catch (ApiRateLimitExceededException ex)
            {
                _logger.LogWarning(ex, "API rate limit exceeded while fetching data for symbol: {Symbol}", symbol);
                
            }
            catch (InvalidApiResponseException ex)
            {
                _logger.LogError(ex, "Invalid API response while fetching data for symbol: {Symbol}", symbol);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching and saving stock data for symbol: {Symbol}", symbol);
            }
        }

    }
}