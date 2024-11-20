using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;

namespace DatabaseProjectAPI.Services;

public class NewsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NewsBackgroundService> _logger;

    public NewsBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NewsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NewsBackgroundService started at: {Time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            var currentTime = DateTime.UtcNow;

            if (currentTime.Hour == 22 && currentTime.Minute == 0)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DpapiDbContext>();
                    var newsService = scope.ServiceProvider.GetRequiredService<INewsAPIService>();

                    var trackedStocks = await dbContext.TrackedStocks.ToListAsync(stoppingToken);

                    foreach (var trackedStock in trackedStocks)
                    {
                        _logger.LogInformation("Fetching news for stock symbol: {Symbol}", trackedStock.Symbol);

                        try
                        {
                            // Fetch news for the stock symbol
                            var newsArticles = await newsService.GetNewsDataAsync(
                                trackedStock.StockName,
                                DateTime.UtcNow.AddDays(-1),
                                DateTime.UtcNow
                            );

                            // Save news articles to the database
                            foreach (var article in newsArticles.Take(1)) // Limit to 1 article per stock
                            {
                                var marketNews = new MarketNews
                                {
                                    StockId = trackedStock.Id,
                                    Headline = article.Title,
                                    SourceUrl = article.Url,
                                    Datetime = article.PublishedAt
                                };

                                dbContext.MarketNews.Add(marketNews);
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("News for stock {Symbol} saved successfully.", trackedStock.Symbol);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "An error occurred while fetching or saving news for stock: {Symbol}", trackedStock.Symbol);
                        }
                    }
                }

                // Delay to prevent frequent execution
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

