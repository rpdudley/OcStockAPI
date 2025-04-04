using DatabaseProjectAPI.DataContext;

namespace DatabaseProjectAPI.Services
{
    public class EventsBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventsBackgroundService> _logger;

        public EventsBackgroundService(IServiceProvider serviceProvider, ILogger<EventsBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EventsBackgroundService started at: {Time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        // Resolve scoped dependencies
                        var dbContext = scope.ServiceProvider.GetRequiredService<DpapiDbContext>();
                        var alphaVantageService = scope.ServiceProvider.GetRequiredService<IAlphaVantageService>();

                        // Fetch and save data
                        await FetchAndSaveInflationDataAsync(dbContext, alphaVantageService, stoppingToken);
                        await FetchAndSaveFederalInterestRateAsync(dbContext, alphaVantageService, stoppingToken);
                        await FetchAndSaveUnemploymentRateAsync(dbContext, alphaVantageService, stoppingToken);
                        await FetchAndSaveCPIAsync(dbContext, alphaVantageService, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred in EventsBackgroundService.");
                    }
                }

                // Wait for 24 hours before fetching data again
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("EventsBackgroundService stopped.");
        }

        private async Task FetchAndSaveInflationDataAsync(IDpapiDbContext dbContext, IAlphaVantageService alphaVantageService, CancellationToken stoppingToken)
        {
            try
            {
                var inflationData = await alphaVantageService.GetInflationAsync();

                if (inflationData?.Data?.Any() == true)
                {
                    var latestData = inflationData.Data.OrderByDescending(d => DateTime.Parse(d.Date)).First();

                    var eventEntity = await GetOrCreateEventAsync(dbContext, DateTime.Parse(latestData.Date), stoppingToken);

                    // Update only the Inflation field
                    eventEntity.Inflation = decimal.Parse(latestData.Value);

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Inflation data updated for date {Date}.", latestData.Date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch or save inflation data.");
            }
        }

        private async Task FetchAndSaveFederalInterestRateAsync(IDpapiDbContext dbContext, IAlphaVantageService alphaVantageService, CancellationToken stoppingToken)
        {
            try
            {
                var rateData = await alphaVantageService.GetFederalInterestRateAsync();

                if (rateData?.Data?.Any() == true)
                {
                    var latestData = rateData.Data.OrderByDescending(d => DateTime.Parse(d.Date)).First();

                    var eventEntity = await GetOrCreateEventAsync(dbContext, DateTime.Parse(latestData.Date), stoppingToken);

                    eventEntity.FederalInterestRate = decimal.Parse(latestData.Value);

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Federal interest rate data updated for date {Date}.", latestData.Date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch or save federal interest rate data.");
            }
        }

        private async Task FetchAndSaveUnemploymentRateAsync(IDpapiDbContext dbContext, IAlphaVantageService alphaVantageService, CancellationToken stoppingToken)
        {
            try
            {
                var unemploymentData = await alphaVantageService.GetUnemploymentRateAsync();

                if (unemploymentData?.Data?.Any() == true)
                {
                    var latestData = unemploymentData.Data.OrderByDescending(d => DateTime.Parse(d.Date)).First();

                    var eventEntity = await GetOrCreateEventAsync(dbContext, DateTime.Parse(latestData.Date), stoppingToken);

                    eventEntity.UnemploymentRate = decimal.Parse(latestData.Value);

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Unemployment rate data updated for date {Date}.", latestData.Date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch or save unemployment rate data.");
            }
        }

        private async Task FetchAndSaveCPIAsync(IDpapiDbContext dbContext, IAlphaVantageService alphaVantageService, CancellationToken stoppingToken)
        {
            try
            {
                var cpiData = await alphaVantageService.GetCPIdataAsync();

                if (cpiData?.Data?.Any() == true)
                {
                    var latestData = cpiData.Data.OrderByDescending(d => DateTime.Parse(d.Date)).First();

                    // Get existing event or create a new one
                    var eventEntity = await GetOrCreateEventAsync(dbContext, DateTime.Parse(latestData.Date), stoppingToken);

                    // Update only the CPI field
                    eventEntity.CPI = decimal.Parse(latestData.Value);

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("CPI data updated for date {Date}.", latestData.Date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch or save CPI data.");
            }
        }

        private async Task<Event> GetOrCreateEventAsync(IDpapiDbContext dbContext, DateTime date, CancellationToken stoppingToken)
        {
            // Try to find an existing event for the given date
            var existingEvent = await dbContext.Events.FirstOrDefaultAsync(e => e.Datetime.HasValue && e.Datetime.Value.Date == date.Date, stoppingToken);

            if (existingEvent != null)
            {
                // Return the existing event to update it
                return existingEvent;
            }
            else
            {
                // Create a new event if none exists
                var newEvent = new Event
                {
                    Datetime = date,
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.Events.AddAsync(newEvent, stoppingToken);

                return newEvent;
            }
        }
    }
}