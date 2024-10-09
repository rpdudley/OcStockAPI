using Microsoft.AspNetCore.Mvc;
using DatabaseProjectAPI.Services;
using System;
using System.Threading.Tasks;

namespace DatabaseProjectAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FinnhubServiceController : ControllerBase
    {
        private readonly IFinnhubService _finnhubService;

        public FinnhubServiceController(IFinnhubService finnhubService)
        {
            _finnhubService = finnhubService;
        }

        [HttpGet("price")]
        public async Task<IActionResult> GetStockPrice(string symbol, DateTime fromDate, DateTime toDate)
        {
            var stockData = await _finnhubService.GetStockDataAsync(symbol, fromDate, toDate);
            return Ok(stockData);
        }
        [HttpGet("market_status")]
        public async Task<IActionResult> MarkStatus()
        {
            var status = await _finnhubService.MarkStatusAsync();
            return Ok(status.isOpen);
        }
    }
}

