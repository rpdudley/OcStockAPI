using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;

namespace DatabaseProjectAPI.Actions
{
    public interface IInvestorAccountAction
    {
        List<InvestorAccount> GetAccounts();
    }
    public class InvestorAccountAction : IInvestorAccountAction
    {
        private readonly DpapiDbContext _dpapiDbContext;

        public InvestorAccountAction(DpapiDbContext dpapiDbContext)
        {
            _dpapiDbContext = dpapiDbContext;
        }
        public List<InvestorAccount> GetAccounts()
        {
            return _dpapiDbContext.InvestorAccounts.OrderByDescending(a => a.Name).ToList();
        }
    }
}