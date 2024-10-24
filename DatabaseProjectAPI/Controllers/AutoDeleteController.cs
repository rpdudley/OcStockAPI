using DatabaseProjectAPI.Actions;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutoDeleteController : ControllerBase
    {
        private readonly IAutoDeleteService _autoDeleteService;

        public AutoDeleteController(IAutoDeleteService autoDeleteService)
        {
            _autoDeleteService = autoDeleteService;
        }

        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupOldData()
        {
            await _autoDeleteService.DeleteOldStockHistory();
            await _autoDeleteService.DeleteOldApiCallLogs();

            return Ok(new { message = "Old data cleanup completed." });
        }
    }
}