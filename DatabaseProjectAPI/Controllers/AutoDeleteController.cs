using DatabaseProjectAPI.Actions;
using DatabaseProjectAPI.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutoDeleteController : ControllerBase
    {
        private readonly IAutoDeleteService _autoDeleteService;
        private readonly IApiRequestLogger _apiRequestLogger;

        public AutoDeleteController(IAutoDeleteService autoDeleteService, IApiRequestLogger apiRequestLogger)
        {
            _autoDeleteService = autoDeleteService;
            _apiRequestLogger = apiRequestLogger;
        }

        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupOldData()
        {
            await _autoDeleteService.DeleteOldStockHistory();
            await _autoDeleteService.DeleteOldApiCallLogs();

            return Ok(new { message = "Old data cleanup completed." });
        }
        [HttpPost("log")]
        public async Task<IActionResult> TestLogApiCall()
        {
            await _apiRequestLogger.LogApiCall("TestCall", "AAPL");
            return Ok("Test log added.");
        }
    }
}