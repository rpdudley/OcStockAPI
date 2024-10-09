using DatabaseProjectAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DatabaseProjectAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AlphaVantageController : ControllerBase
    {
        private readonly IAlphaVantageService _alphaVantageService;

        public AlphaVantageController(IAlphaVantageService alphaVantageService)
        {
            _alphaVantageService = alphaVantageService;
        }

        [HttpGet("global_quote")]
        public async Task<IActionResult> GetGlobalQuote(string symbol)
        {
            try
            {
                var (open, price, volume, latestTradingDay) = await _alphaVantageService.GetStockQuote(symbol);

                if (open == 0 && price == 0 && volume == 0 && latestTradingDay == DateTime.MinValue)
                {
                    return NotFound(new { Message = "No data found for the specified symbol." });
                }

                return Ok(new
                {
                    Symbol = symbol,
                    Open = open,
                    Price = price,
                    Volume = volume,
                    LatestTradingDay = latestTradingDay
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving stock data.", Details = ex.Message });
            }
        }
    }
}

