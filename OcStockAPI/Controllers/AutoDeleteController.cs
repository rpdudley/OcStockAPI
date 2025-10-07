using System;
using System.Threading.Tasks;
using OcStockAPI.Actions;
using OcStockAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace OcStockAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutoDeleteController : ControllerBase
    {
        private readonly IAutoDeleteService _autoDeleteService;
        private readonly IApiRequestLogger _apiRequestLogger;
        private readonly ILogger<AutoDeleteController> _logger;

        public AutoDeleteController(
            IAutoDeleteService autoDeleteService,
            IApiRequestLogger apiRequestLogger,
            ILogger<AutoDeleteController> logger)
        {
            _autoDeleteService = autoDeleteService;
            _apiRequestLogger = apiRequestLogger;
            _logger = logger;
        }

        [HttpDelete("cleanup")]
        [ProducesResponseType(200)]
        [ProducesResponseType(499)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CleanupOldDataAsync()
        {
            try
            {
                await _autoDeleteService.DeleteOldStockHistoryAsync(HttpContext.RequestAborted);
                await _autoDeleteService.DeleteOldApiCallLogsAsync(HttpContext.RequestAborted);

                return Ok(new { Message = "Old data cleanup completed." });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cleanup operation was cancelled.");
                // 499 is a non-standard status code (Client Closed Request) used by some servers
                return StatusCode(499, new { Message = "Cleanup operation was cancelled by the client." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during data cleanup.");
                return StatusCode(500, new { Message = "An error occurred during data cleanup.", Details = ex.Message });
            }
        }
        [HttpPost("log")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> TestLogApiCallAsync()
        {
            try
            {
                await _apiRequestLogger.LogApiCallAsync("TestCall", "AAPL", HttpContext.RequestAborted);
                return Ok(new { Message = "Test log added." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging the test API call.");
                return StatusCode(500, new { Message = "An error occurred while logging the test API call.", Details = ex.Message });
            }
        }
    }
}
