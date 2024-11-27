using DatabaseProjectAPI.Entities;
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

        [HttpGet("cpi")]
        public async Task<IActionResult> GetCPIAsync()
        {
            try
            {
                CPIdata cpiData = await _alphaVantageService.GetCPIdataAsync();

                if (cpiData == null)
                {
                    return NotFound(new { Message = "No CPI data available." });
                }

                return Ok(cpiData);
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
                return StatusCode(500, new { Message = "An error occurred while retrieving CPI data.", Details = ex.Message });
            }
        }

        [HttpGet("inflation")]
        public async Task<IActionResult> GetInflationAsync()
        {
            try
            {
                Inflation inflationData = await _alphaVantageService.GetInflationAsync();

                if (inflationData == null)
                {
                    return NotFound(new { Message = "No Inflation data available." });
                }

                return Ok(inflationData);
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
                return StatusCode(500, new { Message = "An error occurred while retrieving Inflation data.", Details = ex.Message });
            }
        }

        [HttpGet("federal_interest_rate")]
        public async Task<IActionResult> GetFederalInterestRateAsync()
        {
            try
            {
                FederalInterestRate federalRateData = await _alphaVantageService.GetFederalInterestRateAsync();

                if (federalRateData == null)
                {
                    return NotFound(new { Message = "No Federal Interest Rate data available." });
                }

                return Ok(federalRateData);
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
                return StatusCode(500, new { Message = "An error occurred while retrieving Federal Interest Rate data.", Details = ex.Message });
            }
        }

        [HttpGet("unemployment_rate")]
        public async Task<IActionResult> GetUnemploymentRateAsync()
        {
            try
            {
                UnemploymentRate unemploymentRateData = await _alphaVantageService.GetUnemploymentRateAsync();

                if (unemploymentRateData == null)
                {
                    return NotFound(new { Message = "No Unemployment Rate data available." });
                }

                return Ok(unemploymentRateData);
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
                return StatusCode(500, new { Message = "An error occurred while retrieving Unemployment Rate data.", Details = ex.Message });
            }
        }
    }
}