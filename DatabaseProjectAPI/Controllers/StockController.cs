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
    }
}

