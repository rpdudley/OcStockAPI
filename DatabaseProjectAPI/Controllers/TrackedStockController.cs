using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackedStockController : ControllerBase
    {
        private readonly ITrackedStockAction _trackedStockAction;

        public TrackedStockController(ITrackedStockAction trackedStockAction)
        {
            _trackedStockAction = trackedStockAction;
        }
        [HttpGet]
        public IActionResult GetTrackedStocks([FromQuery] string symbol = "")
        {
            var stocks = _trackedStockAction.GetTrackedStocks(symbol);
            return Ok(stocks);
        }

        [HttpPost]
        public IActionResult AddTrackedStock([FromBody] TrackedStock stock)
        {
            _trackedStockAction.AddTrackedStock(stock);
            return Ok();
        }
    }
}
