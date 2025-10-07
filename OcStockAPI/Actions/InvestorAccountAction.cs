using OcStockAPI.DataContext;
using OcStockAPI.Entities;

namespace OcStockAPI.Actions
{
    public interface IInvestorAccountAction
    {
        List<InvestorAccount> GetAccounts();
    }
    public class InvestorAccountAction : IInvestorAccountAction
    {
        private readonly OcStockDbContext _OcStockDbContext;

        public InvestorAccountAction(OcStockDbContext OcStockDbContext)
        {
            _OcStockDbContext = OcStockDbContext;
        }
        public List<InvestorAccount> GetAccounts()
        {
            return _OcStockDbContext.InvestorAccounts.OrderByDescending(a => a.Name).ToList();
        }
    }
}
