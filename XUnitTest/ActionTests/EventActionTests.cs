namespace XUnitTests.ActionTests;
public class EventsActionTests
{
    private readonly DpapiDbContext _dbContext;
    private readonly EventsAction _eventsAction;

    public EventsActionTests()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        _dbContext.Events.AddRange(new List<Event>
        {
            new Event
            {
                Datetime = new DateTime(2024, 10, 1),
                CreatedAt = new DateTime(2024, 10, 2),
                FederalInterestRate = 5.25m,
                UnemploymentRate = 3.8m,
                Inflation = 3.2m,
                CPI = 302.1m,
            },
            new Event
            {
                Datetime = new DateTime(2024, 11, 1),
                CreatedAt = new DateTime(2024, 11, 2),
                FederalInterestRate = 5.5m,
                UnemploymentRate = 3.6m,
                Inflation = 3.5m,
                CPI = 305.3m,
            }
        });

        _dbContext.SaveChanges();

        _eventsAction = new EventsAction(_dbContext);
    }

    [Fact]
    public async Task GetLatestEvent_ReturnsMostRecent()
    {
        var latest = await _eventsAction.GetLatestEvent();
        Assert.NotNull(latest);
        Assert.Equal(new DateTime(2024, 11, 1), latest.Datetime);
    }

    [Fact]
    public async Task GetFederalInterestRate_ReturnsLatestValue()
    {
        var rate = await _eventsAction.GetFederalInterestRate();
        Assert.Equal(5.5m, rate);
    }

    [Fact]
    public async Task GetUnemploymentRate_ReturnsLatestValue()
    {
        var rate = await _eventsAction.GetUnemploymentRate();
        Assert.Equal(3.6m, rate);
    }

    [Fact]
    public async Task GetInflation_ReturnsLatestPositiveValue()
    {
        var inflation = await _eventsAction.GetInflation();
        Assert.Equal(3.5m, inflation);
    }

    [Fact]
    public async Task GetCPI_ReturnsLatestValue()
    {
        var cpi = await _eventsAction.GetCPI();
        Assert.Equal(305.3m, cpi);
    }
}