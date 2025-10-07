using OcStockAPI.Actions;
using Microsoft.AspNetCore.Mvc;

namespace OcStockAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockHistoryController : ControllerBase
    {
        private readonly IStockHistoryAction _stockHistoryAction;

        public StockHistoryController(IStockHistoryAction stockHistoryAction)
        {
            _stockHistoryAction = stockHistoryAction;
        }

        [HttpGet]
        public IActionResult GetStockHistory(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { Message = "Stock symbol is required." });
            }

            var stockHistory = _stockHistoryAction.GetStockHistory(symbol);

            if (stockHistory == null || stockHistory.Count == 0)
            {
                return NotFound(new { Message = $"No history found for the stock symbol: {symbol}." });
            }

            return Ok(stockHistory);
        }

        [HttpGet("range")]
        public IActionResult GetStockHistoryInRange(
            string symbol,
            [FromQuery] string fromDate,
            [FromQuery] string toDate)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { Message = "Stock symbol is required." });
            }

            if (!DateTime.TryParse(fromDate, out var fromDateTime) ||
                !DateTime.TryParse(toDate, out var toDateTime))
            {
                return BadRequest(new { Message = "Invalid date format. Please use yyyy-MM-dd." });
            }

            var stockHistory = _stockHistoryAction.GetStockHistory(symbol, fromDateTime, toDateTime);

            if (stockHistory == null || stockHistory.Count == 0)
            {
                return NotFound(new { Message = $"No history found for the stock symbol: {symbol} between {fromDate} and {toDate}." });
            }

            return Ok(stockHistory);
        }
    }
}

