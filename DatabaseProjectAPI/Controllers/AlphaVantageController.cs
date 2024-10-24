using DatabaseProjectAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DatabaseProjectAPI.Controllers
{
    [Route("api/[controller]")]
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
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { Message = "Stock symbol is required." });
            }

            try
            {
                var (retrievedSymbol, open, price, volume, latestTradingDay) = await _alphaVantageService.GetStockQuote(symbol);

                if (string.IsNullOrWhiteSpace(retrievedSymbol) || open == 0 && price == 0 && volume == 0 && latestTradingDay == DateTime.MinValue)
                {
                    return NotFound(new { Message = $"No data found for the symbol: {symbol}." });
                }

                return Ok(new
                {
                    Symbol = retrievedSymbol,
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
