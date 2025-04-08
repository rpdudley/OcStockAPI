using DatabaseProjectAPI.Actions;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventsAction _eventsAction;

        public EventsController(IEventsAction eventsAction)
        {
            _eventsAction = eventsAction;
        }

        [HttpGet("federal-interest-rate")]
        public async Task<IActionResult> GetFederalInterestRate()
        {
            var rate = await _eventsAction.GetFederalInterestRate();
            if (rate.HasValue)
                return Ok(new { FederalInterestRate = rate });

            return NotFound(new { Message = "No Federal Interest Rate data available." });
        }

        [HttpGet("unemployment-rate")]
        public async Task<IActionResult> GetUnemploymentRate()
        {
            var rate = await _eventsAction.GetUnemploymentRate();
            if (rate.HasValue)
                return Ok(new { UnemploymentRate = rate });

            return NotFound(new { Message = "No Unemployment Rate data available." });
        }

        [HttpGet("inflation")]
        public async Task<IActionResult> GetInflation()
        {
            var inflation = await _eventsAction.GetInflation();
            if (inflation.HasValue)
                return Ok(new { Inflation = inflation });

            return NotFound(new { Message = "No Inflation data available." });
        }

        [HttpGet("cpi")]
        public async Task<IActionResult> GetCPI()
        {
            var cpi = await _eventsAction.GetCPI();
            if (cpi.HasValue)
                return Ok(new { CPI = cpi });

            return NotFound(new { Message = "No CPI data available." });
        }
    }
}