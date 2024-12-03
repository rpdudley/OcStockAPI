using DatabaseProjectAPI.DataContext;
using DatabaseProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseProjectAPI.Actions
{
    public interface IEventsAction
    {
        Task<Event> GetLatestEvent();
        Task<decimal?> GetFederalInterestRate();
        Task<decimal?> GetUnemploymentRate();
        Task<decimal?> GetInflation();
        Task<decimal?> GetCPI();
    }

    public class EventsAction : IEventsAction
    {
        private readonly DpapiDbContext _dbContext;

        public EventsAction(DpapiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Event> GetLatestEvent()
        {
            return await _dbContext.Events
                .OrderByDescending(e => e.Datetime)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal?> GetFederalInterestRate()
        {
            var eventRecord = await _dbContext.Events
                .Where(e => e.FederalInterestRate.HasValue)
                .OrderByDescending(e => e.Datetime)
                .FirstOrDefaultAsync();

            return eventRecord?.FederalInterestRate;
        }

        public async Task<decimal?> GetUnemploymentRate()
        {
            var eventRecord = await _dbContext.Events
                .Where(e => e.UnemploymentRate.HasValue)
                .OrderByDescending(e => e.Datetime)
                .FirstOrDefaultAsync();

            return eventRecord?.UnemploymentRate;
        }

        public async Task<decimal?> GetInflation()
        {
            var eventRecord = await _dbContext.Events
                .Where(e => e.Inflation.HasValue && e.Inflation > 0) 
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Datetime)
                .FirstOrDefaultAsync(); 

            return eventRecord?.Inflation; 
        }

        public async Task<decimal?> GetCPI()
        {
            var eventRecord = await _dbContext.Events
                .Where(e => e.CPI.HasValue)
                .OrderByDescending(e => e.Datetime)
                .FirstOrDefaultAsync();

            return eventRecord?.CPI;
        }
    }
}
