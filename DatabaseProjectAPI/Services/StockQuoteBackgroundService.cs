using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using DatabaseProjectAPI.Helpers;

namespace DatabaseProjectAPI.Services;

public class StockQuoteBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockQuoteBackgroundService> _logger;
    private readonly IFinnhubService _finnhubService;

    public StockQuoteBackgroundService(IServiceProvider serviceProvider, ILogger<StockQuoteBackgroundService> logger, IFinnhubService finnhubService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _finnhubService = finnhubService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockQuoteBackgroundService started at: {time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DpapiDbContext>();
                var apiRequestLogger = scope.ServiceProvider.GetRequiredService<IApiRequestLogger>();
                var autoDeleteService = scope.ServiceProvider.GetRequiredService<IAutoDeleteService>();

                await autoDeleteService.DeleteOldStockHistory();
                await autoDeleteService.DeleteOldApiCallLogs();

                var trackedStocks = await dbContext.TrackedStocks.ToListAsync();

                var marketStatus = await _finnhubService.MarkStatusAsync();
                if (!marketStatus.isOpen)
                {
                    foreach (var stock in trackedStocks)
                    {
                        if (!await apiRequestLogger.HasMadeApiCallToday("MarketClose", stock.Symbol))
                        {
                            _logger.LogInformation("Market is closed, fetching end-of-day stock data for symbol: {Symbol}", stock.Symbol);
                            await FetchAndSaveStockData(dbContext, apiRequestLogger, "MarketClose", stock.Symbol);
                        }
                    }
                }
            }

            // Adjust delay to reduce frequency if only end-of-day data is needed
            await Task.Delay(TimeSpan.FromHours(8), stoppingToken);
        }
    }

    private async Task FetchAndSaveStockData(DpapiDbContext dbContext, IApiRequestLogger apiRequestLogger, string callType, string symbol)
    {
        _logger.LogInformation("FetchAndSaveStockData started for symbol {Symbol} with call type {CallType}", symbol, callType);

        using (var scope = _serviceProvider.CreateScope())
        {
            var stockService = scope.ServiceProvider.GetRequiredService<IAlphaVantageService>();

            try
            {
                // Check if the stock is tracked in TrackedStocks
                var trackedStock = await dbContext.TrackedStocks.FirstOrDefaultAsync(t => t.Symbol == symbol);
                if (trackedStock != null)
                {
                    // Check if there's an entry in `Stocks` for this tracked stock
                    var stock = await dbContext.Stocks.FirstOrDefaultAsync(s => s.TrackedStockId == trackedStock.Id);

                    // If no entry exists in `Stocks`, create one
                    if (stock == null)
                    {
                        stock = new Stock
                        {
                            TrackedStockId = trackedStock.Id,
                            OpenValue = 0, 
                            ClosingValue = 0, 
                            Volume = 0, 
                            LastUpdated = DateTime.UtcNow
                        };
                        dbContext.Stocks.Add(stock);
                        await dbContext.SaveChangesAsync();
                    }

                    // Fetch data from the API and update `Stocks` and `StockHistory`
                    var stockQuote = await stockService.GetStockQuote(symbol);
                    stock.OpenValue = stockQuote.Open;
                    stock.ClosingValue = stockQuote.Price;
                    stock.Volume = stockQuote.Volume;
                    stock.LastUpdated = DateTime.UtcNow;

                    // Create a new entry in `StockHistory` to track the history
                    var stockHistory = new StockHistory
                    {
                        StockId = stock.StockId,
                        Timestamp = DateTime.UtcNow,
                        OpenedValue = stockQuote.Open,
                        ClosedValue = stockQuote.Price,
                        Volume = stockQuote.Volume
                    };
                    dbContext.StockHistories.Add(stockHistory);

                    // Save changes to both `Stocks` and `StockHistory`
                    await dbContext.SaveChangesAsync();

                    // Log that an API call was successfully made
                    await apiRequestLogger.LogApiCall(callType, symbol);
                    _logger.LogInformation("Stock and history data saved successfully for {Symbol} at {Timestamp}", symbol, DateTime.UtcNow);
                }
                else
                {
                    _logger.LogWarning("Tracked stock not found in the database. Symbol: {Symbol}", symbol);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching and saving stock history data.");
            }
        }
    }
}