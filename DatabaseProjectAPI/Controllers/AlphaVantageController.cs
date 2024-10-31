using DatabaseProjectAPI.Helpers;
using DatabaseProjectAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static DatabaseProjectAPI.Services.AlphaVantageService;

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
        public async Task<IActionResult> GetGlobalQuoteAsync([FromQuery] string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { Message = "Stock symbol is required." });
            }

            try
            {
                StockQuote stockQuote = await _alphaVantageService.GetStockQuoteAsync(symbol);

                if (stockQuote == null)
                {
                    return NotFound(new { Message = $"No data found for the symbol: {symbol}." });
                }

                return Ok(stockQuote);
            }
            catch (ApiRateLimitExceededException ex)
            {
                return StatusCode(429, new { Message = ex.Message });
            }
            catch (InvalidApiResponseException ex)
            {
                return StatusCode(502, new { Message = "Invalid response received from the API.", Details = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving stock data.", Details = ex.Message });
            }
        }
    }
}

