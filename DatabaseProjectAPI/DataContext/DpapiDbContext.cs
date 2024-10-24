using Microsoft.EntityFrameworkCore;
using DatabaseProjectAPI.Entities;

namespace DatabaseProjectAPI.DataContext
{
    public interface IDpapiDbContext
    {
        DbSet<InvestorAccount> InvestorAccounts { get; set; }
        DbSet<Portfolio> Portfolios { get; set; }
        DbSet<MutualFund> MutualFunds { get; set; }
        DbSet<Stock> Stocks { get; set; }
        DbSet<StockHistory> StockHistories { get; set; }
        DbSet<Event> Events { get; set; }
        DbSet<MarketNews> MarketNews { get; set; }
        DbSet<PortfolioMutualFund> PortfolioMutualFunds { get; set; }
        DbSet<PortfolioStock> PortfolioStocks { get; set; }
        DbSet<EventStock> EventStocks { get; set; }
        DbSet<EventMutualFund> EventMutualFunds { get; set; }
        DbSet<TrackedStock> TrackedStocks { get; set; }

    }

    public class DpapiDbContext : DbContext, IDpapiDbContext
    {
        public DpapiDbContext(DbContextOptions<DpapiDbContext> options) : base(options)
        {
        }

        public DbSet<InvestorAccount> InvestorAccounts { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<MutualFund> MutualFunds { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<StockHistory> StockHistories { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<MarketNews> MarketNews { get; set; }
        public DbSet<PortfolioMutualFund> PortfolioMutualFunds { get; set; }
        public DbSet<PortfolioStock> PortfolioStocks { get; set; }
        public DbSet<EventStock> EventStocks { get; set; }
        public DbSet<EventMutualFund> EventMutualFunds { get; set; }
        public DbSet<TrackedStock> TrackedStocks { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PortfolioMutualFund>()
                .HasKey(pm => new { pm.PortfolioId, pm.MutualFundId });

            modelBuilder.Entity<PortfolioStock>()
                .HasKey(ps => new { ps.PortfolioId, ps.StockId });

            modelBuilder.Entity<EventStock>()
                .HasKey(es => new { es.EventId, es.StockId });

            modelBuilder.Entity<EventMutualFund>()
                .HasKey(em => new { em.EventId, em.MutualFundId });
        }
    }
}