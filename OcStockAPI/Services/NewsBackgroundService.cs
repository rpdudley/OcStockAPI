using OcStockAPI.DataContext;
using OcStockAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OcStockAPI.Services
{
    public class NewsBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NewsBackgroundService> _logger;
        private readonly INewsAPIService _newsAPIService;

        public NewsBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NewsBackgroundService> logger,
            INewsAPIService newsAPIService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _newsAPIService = newsAPIService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NewsBackgroundService started at: {Time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                var currentTime = DateTime.UtcNow;
                var nextRunTime = currentTime.Date.AddHours(22);

                if (currentTime >= nextRunTime)
                {
                    nextRunTime = nextRunTime.AddDays(1);
                }

                var delay = nextRunTime - currentTime;
                _logger.LogInformation("Next execution scheduled at: {Time}", nextRunTime);

                // Delay until the next execution time
                await Task.Delay(delay, stoppingToken);

                // Perform the work
                await FetchAndSaveNewsAsync(stoppingToken);

                _logger.LogInformation("NewsBackgroundService has completed its execution.");
            }
        }

        public async Task FetchAndSaveNewsAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OcStockDbContext>();

                var trackedStocks = await dbContext.TrackedStocks.ToListAsync(stoppingToken);

                foreach (var trackedStock in trackedStocks)
                {
                    try
                    {
                        _logger.LogInformation("Fetching news for stock symbol: {Symbol}", trackedStock.Symbol);

                        // Fetch news articles using NewsAPIService
                        var fromDate = DateTime.UtcNow.AddDays(-1);
                        var toDate = DateTime.UtcNow;
                        var newsArticles = await _newsAPIService.GetNewsDataAsync(trackedStock.StockName, fromDate, toDate);

                        // Retrieve the corresponding Stock entity
                        var stock = await dbContext.Stocks
                            .FirstOrDefaultAsync(s => s.Symbol == trackedStock.Symbol, stoppingToken);

                        if (stock == null)
                        {
                            // Handle missing Stock
                            _logger.LogError("Stock with symbol {Symbol} not found in Stocks table.", trackedStock.Symbol);
                            continue; // Skip to the next iteration
                        }

                        // Create MarketNews entries
                        foreach (var article in newsArticles)
                        {
                            // Check if the news article already exists to avoid duplicates
                            bool exists = await dbContext.MarketNews.AnyAsync(mn => mn.SourceUrl == article.Url, stoppingToken);
                            if (exists)
                            {
                                continue; // Skip existing news articles
                            }

                            var marketNews = new MarketNews
                            {
                                StockId = stock.StockId, // Use existing StockId
                                Headline = article.Title,
                                SourceUrl = article.Url,
                                Datetime = article.PublishedAt
                            };

                            dbContext.MarketNews.Add(marketNews);
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        _logger.LogError(ex, "An error occurred while fetching or saving news for stock: {Symbol}", trackedStock.Symbol);
                    }
                }
            }
        }
    }
}
