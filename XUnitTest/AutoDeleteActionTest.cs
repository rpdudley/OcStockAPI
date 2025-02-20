using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace XUnitTest;

public class AutoDeleteActionTest
{
    private List<StockHistory> MockStockHistoryList()
    {
        return new List<StockHistory>
        {
            new StockHistory {HistoryId=1, Timestamp = DateTime.UtcNow.AddDays(-95)},
            new StockHistory {HistoryId =2, Timestamp = DateTime.UtcNow.AddDays(-100)}

        };
    }

    [Fact]
    public async Task DeleteOldStockHistoryTest_ShouldDelete()
    {
        var mockDbContext = new Mock<DpapiDbContext>();
        var mockLogger = new Mock<ILogger<AutoDeleteAction>>();
        var cancellationToken = new CancellationToken();
        var service = new AutoDeleteAction(mockDbContext.Object, mockLogger.Object);

        var stockHistoryList = MockStockHistoryList();
        var mockStockHistoryDbSet = new Mock<DbSet<StockHistory>>();

        mockDbContext.Setup(db => db.StockHistories).Returns(mockStockHistoryDbSet.Object);
        mockStockHistoryDbSet.Setup(db => db.ExecuteDeleteAsync(cancellationToken)).ReturnsAsync(stockHistoryList.Count);

        var result = service.DeleteOldStockHistoryAsync(cancellationToken);

        Assert.NotNull(result);

        mockLogger.Verify(x => x.LogInformation("{Count} old stock history records deleted.", stockHistoryList.Count), Times.Once);
    }
}