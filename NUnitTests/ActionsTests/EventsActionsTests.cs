namespace NUnitTests.ActionsTests;

public class EventsActionTests
{
    private DpapiDbContext _dbContext;
    private EventsAction _eventsAction;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DpapiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DpapiDbContext(options);

        _dbContext.Events.AddRange(new List<Event>
        {
            new Event
            {
                EventId = 1,
                Datetime = new DateTime(2024, 10, 1),
                CreatedAt = new DateTime(2024, 10, 2),
                FederalInterestRate = 5.25m,
                UnemploymentRate = 3.8m,
                Inflation = 3.2m,
                CPI = 302.1m,
                EventStocks = new List<EventStock>()
            },
            new Event
            {
                EventId = 2,
                Datetime = new DateTime(2024, 11, 1),
                CreatedAt = new DateTime(2024, 11, 2),
                FederalInterestRate = 5.5m,
                UnemploymentRate = 3.6m,
                Inflation = 3.5m,
                CPI = 305.3m,
                EventStocks = new List<EventStock>()
            }
        });

        _dbContext.SaveChanges();
        _eventsAction = new EventsAction(_dbContext);
    }

    [TearDown]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetLatestEvent_ReturnsMostRecentByDatetime()
    {
        var latestEvent = await _eventsAction.GetLatestEvent();
        Assert.NotNull(latestEvent);
        Assert.AreEqual(new DateTime(2024, 11, 1), latestEvent.Datetime);
    }

    [Test]
    public async Task GetFederalInterestRate_ReturnsLatestValue()
    {
        var rate = await _eventsAction.GetFederalInterestRate();
        Assert.AreEqual(5.5m, rate);
    }

    [Test]
    public async Task GetUnemploymentRate_ReturnsLatestValue()
    {
        var rate = await _eventsAction.GetUnemploymentRate();
        Assert.AreEqual(3.6m, rate);
    }

    [Test]
    public async Task GetInflation_ReturnsLatestPositiveInflation()
    {
        var inflation = await _eventsAction.GetInflation();
        Assert.AreEqual(3.5m, inflation);
    }

    [Test]
    public async Task GetCPI_ReturnsLatestValue()
    {
        var cpi = await _eventsAction.GetCPI();
        Assert.AreEqual(305.3m, cpi);
    }
}
