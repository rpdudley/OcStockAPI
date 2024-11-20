using DatabaseProjectAPI.Actions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DatabaseProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IStockAction _stockAction;

        public StockController(IStockAction stockAction)
        {
            _stockAction = stockAction;
        }

        // GET: api/stock/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStockById(int id)
        {
            var stock = await _stockAction.GetStocksById(id);
            if (stock == null)
            {
                return NotFound();
            }
            return Ok(stock);
        }

        // GET: api/stock
        [HttpGet]
        public async Task<IActionResult> GetAllStocks()
        {
            var stocks = await _stockAction.GetAllStocks();
            return Ok(stocks);
        }

        // GET: api/stock/symbol/{symbol}
        [HttpGet("symbol/{symbol}")]
        public async Task<IActionResult> GetStocksBySymbol(string symbol)
        {
            var stocks = await _stockAction.GetStocksBySymbol(symbol);
            if (stocks == null || stocks.Count == 0)
            {
                return NotFound(new { Message = $"No stocks found for symbol: {symbol}" });
            }
            return Ok(stocks);
        }
    }
}


