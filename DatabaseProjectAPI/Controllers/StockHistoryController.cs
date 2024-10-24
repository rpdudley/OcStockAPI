using DatabaseProjectAPI.Actions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DatabaseProjectAPI.Controllers
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
    }
}
