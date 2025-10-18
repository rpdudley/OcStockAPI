using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OcStockAPI.Services;
using OcStockAPI.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace OcStockAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all endpoints
    [SwaggerTag("Tracked stocks management - requires authentication")]
    public class TrackedStocksController : ControllerBase
    {
        private readonly ITrackedStockService _trackedStockService;
        private readonly ILogger<TrackedStocksController> _logger;

        public TrackedStocksController(ITrackedStockService trackedStockService, ILogger<TrackedStocksController> logger)
        {
            _trackedStockService = trackedStockService;
            _logger = logger;
        }

        /// <summary>
        /// Get all tracked stocks
        /// </summary>
        /// <returns>List of all tracked stocks</returns>
        [HttpGet]
        [SwaggerOperation(Summary = "Get all tracked stocks", Description = "Retrieves all currently tracked stocks (maximum 20)")]
        [SwaggerResponse(200, "Successfully retrieved tracked stocks", typeof(TrackedStockResponse))]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<ActionResult<TrackedStockResponse>> GetAllTrackedStocks()
        {
            try
            {
                var result = await _trackedStockService.GetAllTrackedStocksAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tracked stocks");
                return StatusCode(500, new TrackedStockResponse 
                { 
                    Success = false, 
                    Message = "Internal server error" 
                });
            }
        }

        /// <summary>
        /// Add a new stock to track
        /// </summary>
        /// <param name="request">Stock details to add</param>
        /// <returns>Result of the add operation</returns>
        [HttpPost]
        [SwaggerOperation(Summary = "Add stock to tracking list", Description = "Adds a new stock to the tracking list (maximum 20 stocks)")]
        [SwaggerResponse(201, "Stock successfully added to tracking list", typeof(TrackedStockResponse))]
        [SwaggerResponse(400, "Invalid request or limit reached")]
        [SwaggerResponse(409, "Stock symbol already tracked")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<ActionResult<TrackedStockResponse>> AddTrackedStock([FromBody] AddTrackedStockRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new TrackedStockResponse 
                { 
                    Success = false, 
                    Message = "Invalid request data" 
                });
            }

            try
            {
                var result = await _trackedStockService.AddTrackedStockAsync(request);
                
                if (!result.Success)
                {
                    if (result.Message.Contains("already being tracked"))
                        return Conflict(result);
                    
                    if (result.Message.Contains("Maximum limit"))
                        return BadRequest(result);
                }

                return result.Success ? Created($"/api/trackedstocks/{result.Data?.Id}", result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tracked stock: {Symbol}", request.Symbol);
                return StatusCode(500, new TrackedStockResponse 
                { 
                    Success = false, 
                    Message = "Internal server error" 
                });
            }
        }

        /// <summary>
        /// Update a tracked stock's details
        /// </summary>
        /// <param name="id">ID of the tracked stock</param>
        /// <param name="request">Updated stock details</param>
        /// <returns>Result of the update operation</returns>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update tracked stock", Description = "Updates the details of a tracked stock")]
        [SwaggerResponse(200, "Stock successfully updated", typeof(TrackedStockResponse))]
        [SwaggerResponse(404, "Stock not found")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<ActionResult<TrackedStockResponse>> UpdateTrackedStock(int id, [FromBody] UpdateTrackedStockRequest request)
        {
            try
            {
                var result = await _trackedStockService.UpdateTrackedStockAsync(id, request);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracked stock: {Id}", id);
                return StatusCode(500, new TrackedStockResponse 
                { 
                    Success = false, 
                    Message = "Internal server error" 
                });
            }
        }

        /// <summary>
        /// Remove a stock from tracking by ID
        /// </summary>
        /// <param name="id">ID of the tracked stock to remove</param>
        /// <returns>Result of the remove operation</returns>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Remove tracked stock by ID", Description = "Removes a stock from the tracking list by its ID")]
        [SwaggerResponse(200, "Stock successfully removed from tracking")]
        [SwaggerResponse(404, "Stock not found")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<ActionResult<TrackedStockResponse>> RemoveTrackedStock(int id)
        {
            try
            {
                var result = await _trackedStockService.RemoveTrackedStockAsync(id);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tracked stock: {Id}", id);
                return StatusCode(500, new TrackedStockResponse 
                { 
                    Success = false, 
                    Message = "Internal server error" 
                });
            }
        }

        /// <summary>
        /// Remove a stock from tracking by symbol
        /// </summary>
        /// <param name="symbol">Stock symbol to remove</param>
        /// <returns>Result of the remove operation</returns>
        [HttpDelete("symbol/{symbol}")]
        [SwaggerOperation(Summary = "Remove tracked stock by symbol", Description = "Removes a stock from the tracking list by its symbol")]
        [SwaggerResponse(200, "Stock successfully removed from tracking")]
        [SwaggerResponse(404, "Stock not found")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<ActionResult<TrackedStockResponse>> RemoveTrackedStockBySymbol(string symbol)
        {
            try
            {
                var result = await _trackedStockService.RemoveTrackedStockBySymbolAsync(symbol);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tracked stock by symbol: {Symbol}", symbol);
                return StatusCode(500, new TrackedStockResponse 
                { 
                    Success = false, 
                    Message = "Internal server error" 
                });
            }
        }

        /// <summary>
        /// Get the current count of tracked stocks
        /// </summary>
        /// <returns>Current count and maximum allowed</returns>
        [HttpGet("count")]
        [SwaggerOperation(Summary = "Get tracked stocks count", Description = "Returns the current number of tracked stocks and the maximum allowed")]
        [SwaggerResponse(200, "Successfully retrieved count")]
        public async Task<ActionResult<object>> GetTrackedStockCount()
        {
            try
            {
                var count = await _trackedStockService.GetTrackedStockCountAsync();
                return Ok(new 
                { 
                    currentCount = count, 
                    maxAllowed = 20,
                    remaining = 20 - count,
                    canAddMore = count < 20
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracked stock count");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Check if a symbol is already being tracked
        /// </summary>
        /// <param name="symbol">Stock symbol to check</param>
        /// <returns>Whether the symbol is being tracked</returns>
        [HttpGet("check/{symbol}")]
        [SwaggerOperation(Summary = "Check if symbol is tracked", Description = "Checks if a specific stock symbol is already being tracked")]
        [SwaggerResponse(200, "Successfully checked symbol")]
        public async Task<ActionResult<object>> CheckSymbolTracked(string symbol)
        {
            try
            {
                var isTracked = await _trackedStockService.IsSymbolTrackedAsync(symbol);
                return Ok(new 
                { 
                    symbol = symbol.ToUpperInvariant(),
                    isTracked = isTracked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if symbol is tracked: {Symbol}", symbol);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}