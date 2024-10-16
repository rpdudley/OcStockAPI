using DatabaseProjectAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsAPIController : ControllerBase
    {
        private readonly INewsAPIService _newsAPIService;

        public NewsAPIController(INewsAPIService newsAPIService)
        {
            _newsAPIService = newsAPIService;
        }

        [HttpGet("news")]
        public async Task<IActionResult> GetNews(string name, DateTime from, DateTime to)
        {
            try
            {
                var articles = await _newsAPIService.GetNewsDataAsync(name, from, to);
                return Ok(articles);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { Message = "Error fetching news data.", Details = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Invalid request parameters.", Details = ex.Message });
            }
        }
    }
}