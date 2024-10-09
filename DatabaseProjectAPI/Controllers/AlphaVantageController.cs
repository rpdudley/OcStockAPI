using DatabaseProjectAPI.Services;
using Microsoft.AspNetCore.Mvc;
using DatabaseProjectAPI.Entities;

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
        public async Task<IActionResult> GlobalOpeningPrice(string symbol)
        {
            var quote = await _alphaVantageService.GetStockQuote(symbol);
            return Ok(quote);
        }
    }
}
