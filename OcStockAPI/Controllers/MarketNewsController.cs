using OcStockAPI.Actions;
using Microsoft.AspNetCore.Mvc;

namespace OcStockAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketNewsController : ControllerBase
    {
        private readonly IMarketNewsAction _marketNewsAction;

        public MarketNewsController(IMarketNewsAction marketNewsAction)
        {
            _marketNewsAction = marketNewsAction;
        }

        [HttpGet("byDateAndStock")]
        public async Task<IActionResult> GetMarketNewsByDateAndStockId(DateTime date, int stockId)
        {
            if (stockId <= 0 || date == default)
            {
                return BadRequest(new { Message = "Invalid stockId or date provided." });
            }

            var news = await _marketNewsAction.GetMarketNewsByDateAndStockId(date, stockId);

            if (news == null || !news.Any())
            {
                return NotFound(new { Message = "No market news found for the provided date and stockId." });
            }

            return Ok(news);
        }
    }
}
