using OcStockAPI.DataContext;
using OcStockAPI.Entities;
using OcStockAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace OcStockAPI.Services
{
    public interface ITrackedStockService
    {
        Task<TrackedStockResponse> GetAllTrackedStocksAsync();
        Task<TrackedStockResponse> AddTrackedStockAsync(AddTrackedStockRequest request);
        Task<TrackedStockResponse> UpdateTrackedStockAsync(int id, UpdateTrackedStockRequest request);
        Task<TrackedStockResponse> RemoveTrackedStockAsync(int id);
        Task<TrackedStockResponse> RemoveTrackedStockBySymbolAsync(string symbol);
        Task<int> GetTrackedStockCountAsync();
        Task<bool> IsSymbolTrackedAsync(string symbol);
    }

    public class TrackedStockService : ITrackedStockService
    {
        private readonly OcStockDbContext _dbContext;
        private readonly ILogger<TrackedStockService> _logger;
        private const int MaxTrackedStocks = 20; // Alpha Vantage free tier allows 25 calls/day, keeping 5 for buffer

        public TrackedStockService(OcStockDbContext dbContext, ILogger<TrackedStockService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<TrackedStockResponse> GetAllTrackedStocksAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve tracked stocks from database");
                
                // Test database connectivity first
                var canConnect = await _dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Cannot connect to database");
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = "Database connection failed. Please check your connection string and network connectivity."
                    };
                }
                
                var trackedStocks = await _dbContext.TrackedStocks
                    .OrderBy(ts => ts.DateAdded ?? DateTime.MinValue)
                    .Select(ts => new TrackedStockDto
                    {
                        Id = ts.Id,
                        Symbol = ts.Symbol ?? string.Empty,
                        StockName = ts.StockName,
                        DateAdded = ts.DateAdded ?? DateTime.UtcNow
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} tracked stocks", trackedStocks.Count);
                
                return new TrackedStockResponse
                {
                    Success = true,
                    Message = $"Retrieved {trackedStocks.Count}/{MaxTrackedStocks} tracked stocks",
                    TrackedStocks = trackedStocks
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tracked stocks");
                
                // Provide more specific error message based on exception type
                var errorMessage = ex switch
                {
                    Npgsql.NpgsqlException npgEx => $"Database connection error: {npgEx.Message}",
                    System.Net.Sockets.SocketException sockEx => $"Network connectivity issue: {sockEx.Message}. Please check your internet connection and Supabase database status.",
                    InvalidOperationException invEx => $"Configuration error: {invEx.Message}",
                    _ => $"Unexpected error: {ex.Message}"
                };
                
                return new TrackedStockResponse
                {
                    Success = false,
                    Message = errorMessage
                };
            }
        }

        public async Task<TrackedStockResponse> AddTrackedStockAsync(AddTrackedStockRequest request)
        {
            try
            {
                // Validate symbol format
                var symbol = request.Symbol.ToUpperInvariant().Trim();
                
                _logger.LogInformation("Attempting to add tracked stock: {Symbol}", symbol);
                
                // Test database connectivity first
                var canConnect = await _dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Cannot connect to database for adding stock: {Symbol}", symbol);
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = "Database connection failed. Please check your connection string and network connectivity."
                    };
                }
                
                // Check if already tracking this symbol
                if (await IsSymbolTrackedAsync(symbol))
                {
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = $"Stock symbol '{symbol}' is already being tracked"
                    };
                }

                // Check if we've reached the limit
                var currentCount = await GetTrackedStockCountAsync();
                if (currentCount >= MaxTrackedStocks)
                {
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = $"Cannot add more stocks. Maximum limit of {MaxTrackedStocks} reached. Remove a stock first."
                    };
                }

                var trackedStock = new TrackedStock
                {
                    Symbol = symbol,
                    StockName = request.StockName?.Trim(),
                    DateAdded = DateTime.UtcNow
                };

                _dbContext.TrackedStocks.Add(trackedStock);
                var saveResult = await _dbContext.SaveChangesAsync();
                
                if (saveResult > 0)
                {
                    _logger.LogInformation("Successfully added tracked stock: {Symbol} with ID: {Id}", symbol, trackedStock.Id);

                    return new TrackedStockResponse
                    {
                        Success = true,
                        Message = $"Successfully added '{symbol}' to tracked stocks ({currentCount + 1}/{MaxTrackedStocks})",
                        Data = new TrackedStockDto
                        {
                            Id = trackedStock.Id,
                            Symbol = trackedStock.Symbol ?? string.Empty,
                            StockName = trackedStock.StockName,
                            DateAdded = trackedStock.DateAdded ?? DateTime.UtcNow
                        }
                    };
                }
                else
                {
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = "Failed to save tracked stock to database"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tracked stock: {Symbol}", request.Symbol);
                
                // Provide more specific error message based on exception type
                var errorMessage = ex switch
                {
                    Npgsql.NpgsqlException npgEx => $"Database error: {npgEx.Message}",
                    System.Net.Sockets.SocketException sockEx => $"Network connectivity issue: {sockEx.Message}. Please check your internet connection.",
                    InvalidOperationException invEx => $"Configuration error: {invEx.Message}",
                    _ => $"Unexpected error: {ex.Message}"
                };
                
                return new TrackedStockResponse
                {
                    Success = false,
                    Message = $"Failed to add tracked stock: {errorMessage}"
                };
            }
        }

        public async Task<TrackedStockResponse> UpdateTrackedStockAsync(int id, UpdateTrackedStockRequest request)
        {
            try
            {
                var trackedStock = await _dbContext.TrackedStocks.FindAsync(id);
                if (trackedStock == null)
                {
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = "Tracked stock not found"
                    };
                }

                trackedStock.StockName = request.StockName?.Trim();
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Updated tracked stock: {Symbol}", trackedStock.Symbol);

                return new TrackedStockResponse
                {
                    Success = true,
                    Message = "Successfully updated tracked stock",
                    Data = new TrackedStockDto
                    {
                        Id = trackedStock.Id,
                        Symbol = trackedStock.Symbol ?? string.Empty,
                        StockName = trackedStock.StockName,
                        DateAdded = trackedStock.DateAdded
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracked stock with ID: {Id}", id);
                return new TrackedStockResponse
                {
                    Success = false,
                    Message = "Failed to update tracked stock"
                };
            }
        }

        public async Task<TrackedStockResponse> RemoveTrackedStockAsync(int id)
        {
            try
            {
                var trackedStock = await _dbContext.TrackedStocks.FindAsync(id);
                if (trackedStock == null)
                {
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = "Tracked stock not found"
                    };
                }

                var symbol = trackedStock.Symbol;
                _dbContext.TrackedStocks.Remove(trackedStock);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Removed tracked stock: {Symbol}", symbol);

                var remainingCount = await GetTrackedStockCountAsync();
                return new TrackedStockResponse
                {
                    Success = true,
                    Message = $"Successfully removed '{symbol}' from tracked stocks ({remainingCount}/{MaxTrackedStocks})"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tracked stock with ID: {Id}", id);
                return new TrackedStockResponse
                {
                    Success = false,
                    Message = "Failed to remove tracked stock"
                };
            }
        }

        public async Task<TrackedStockResponse> RemoveTrackedStockBySymbolAsync(string symbol)
        {
            try
            {
                var normalizedSymbol = symbol.ToUpperInvariant().Trim();
                var trackedStock = await _dbContext.TrackedStocks
                    .FirstOrDefaultAsync(ts => ts.Symbol == normalizedSymbol);

                if (trackedStock == null)
                {
                    return new TrackedStockResponse
                    {
                        Success = false,
                        Message = $"Tracked stock '{normalizedSymbol}' not found"
                    };
                }

                _dbContext.TrackedStocks.Remove(trackedStock);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Removed tracked stock: {Symbol}", normalizedSymbol);

                var remainingCount = await GetTrackedStockCountAsync();
                return new TrackedStockResponse
                {
                    Success = true,
                    Message = $"Successfully removed '{normalizedSymbol}' from tracked stocks ({remainingCount}/{MaxTrackedStocks})"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tracked stock by symbol: {Symbol}", symbol);
                return new TrackedStockResponse
                {
                    Success = false,
                    Message = "Failed to remove tracked stock"
                };
            }
        }

        public async Task<int> GetTrackedStockCountAsync()
        {
            try
            {
                // Test database connectivity first
                var canConnect = await _dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogWarning("Cannot connect to database for count");
                    return 0;
                }
                
                return await _dbContext.TrackedStocks.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracked stock count");
                return 0;
            }
        }

        public async Task<bool> IsSymbolTrackedAsync(string symbol)
        {
            var normalizedSymbol = symbol.ToUpperInvariant().Trim();
            return await _dbContext.TrackedStocks
                .AnyAsync(ts => ts.Symbol == normalizedSymbol);
        }
    }
}