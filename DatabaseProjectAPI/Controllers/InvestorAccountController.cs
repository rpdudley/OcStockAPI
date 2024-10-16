using Microsoft.AspNetCore.Mvc;
using DatabaseProjectAPI.Actions;

namespace DatabaseProjectAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InvestorAccountController : ControllerBase
    {
        private readonly IInvestorAccountAction _investorAccountAction;

        public InvestorAccountController(IInvestorAccountAction investorAccountAction)
        {
            _investorAccountAction = investorAccountAction;
        }

        [HttpGet("accounts")]
        public IActionResult GetAccounts()
        {
            var accounts = _investorAccountAction.GetAccounts();
            return Ok(accounts);
        }
    }
}