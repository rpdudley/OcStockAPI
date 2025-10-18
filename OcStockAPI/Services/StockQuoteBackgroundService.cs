using OcStockAPI.Actions;
using OcStockAPI.DataContext;
using OcStockAPI.Entities;
using OcStockAPI.Helpers;
using static OcStockAPI.Services.AlphaVantageService;

namespace OcStockAPI.Services
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

                if (currentTime.Hour == 22 && (currentTime.Minute == 0 || currentTime.Minute == 1))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<OcStockDbContext>();
                        var apiRequestLogger = scope.ServiceProvider.GetRequiredService<IApiRequestLogger>();
                        var autoDeleteService = scope.ServiceProvider.GetRequiredService<IAutoDeleteService>();
                        var trackedStockService = scope.ServiceProvider.GetRequiredService<ITrackedStockService>();

                        // Perform cleanup tasks first
                        await autoDeleteService.DeleteOldStockHistoryAsync(stoppingToken);
                        await autoDeleteService.DeleteOldApiCallLogsAsync(stoppingToken);

                        // Get tracked stocks (max 20)
                        var trackedStocksResponse = await trackedStockService.GetAllTrackedStocksAsync();
                        
                        if (!trackedStocksResponse.Success || trackedStocksResponse.TrackedStocks == null)
                        {
                            _logger.LogWarning("Failed to retrieve tracked stocks or no stocks to track");
                            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                            continue;
                        }

                        var trackedStocks = await dbContext.TrackedStocks
                            .Take(20) // Ensure we never process more than 20 stocks
                            .ToListAsync(stoppingToken);

                        _logger.LogInformation("Processing {Count} tracked stocks (max 20 allowed)", trackedStocks.Count);

                        var marketStatus = await _finnhubService.MarkStatusAsync();
                        if (!marketStatus.isOpen)
                        {
                            int processedCount = 0;
                            int maxDailyCalls = 20; 

                            foreach (var trackedStock in trackedStocks)
                            {
                                if (processedCount >= maxDailyCalls)
                                {
                                    _logger.LogWarning("Reached daily API call limit ({MaxCalls}). Stopping stock processing.", maxDailyCalls);
                                    break;
                                }

                                if (!await apiRequestLogger.HasMadeApiCallTodayAsync("MarketClose", trackedStock.Symbol ?? string.Empty, stoppingToken))
                                {
                                    _logger.LogInformation("Market is closed, fetching end-of-day stock data for symbol: {Symbol} ({ProcessedCount}/{MaxCalls})", 
                                        trackedStock.Symbol, processedCount + 1, maxDailyCalls);
                                    
                                    await FetchAndSaveStockDataAsync(dbContext, apiRequestLogger, "MarketClose", trackedStock, stoppingToken);
                                    processedCount++;
                                    
                                    // Add delay between API calls to respect rate limits
                                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                                }
                            }

                            _logger.LogInformation("Completed processing {ProcessedCount} stocks out of {TotalStocks} tracked stocks", 
                                processedCount, trackedStocks.Count);
                        }
                        else
                        {
                            _logger.LogInformation("Market is open, skipping end-of-day data collection");
                        }
                    }
                    
                    // Wait 24 hours before next run
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }

                // Check every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        private async Task FetchAndSaveStockDataAsync(OcStockDbContext dbContext, IApiRequestLogger apiRequestLogger, string callType, TrackedStock trackedStock, CancellationToken cancellationToken)
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
