using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleaningLogsController : ControllerBase
{
    private readonly ICleaningLogService _cleaningLogService;
    private readonly ILogger<CleaningLogsController> _logger;

    public CleaningLogsController(ICleaningLogService cleaningLogService, ILogger<CleaningLogsController> logger)
    {
        _cleaningLogService = cleaningLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CleaningLog>>> Get()
    {
        _logger.LogInformation("Fetching all cleaning logs at {Time}", DateTime.UtcNow);
        var logs = await _cleaningLogService.GetAllAsync();
        _logger.LogDebug("Retrieved {Count} logs from database", logs.Count);
        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CleaningLog>> Get(string id)
    {
        _logger.LogInformation("Requested cleaning log with ID: {LogId}", id);
        var log = await _cleaningLogService.GetByIdAsync(id);

        if (log == null)
        {
            _logger.LogWarning("Cleaning log {LogId} not found", id);
            return NotFound();
        }

        return Ok(log);
    }

    [HttpPost]
    public async Task<ActionResult<CleaningLog>> Post([FromBody] CleaningLog log)
    {
        _logger.LogInformation("Creating a new manual cleaning log entry");
        try
        {
            var created = await _cleaningLogService.CreateAsync(log);
            _logger.LogInformation("Successfully created log with ID: {LogId}", created.Id);
            return CreatedAtAction(nameof(Get), new { id = created.Id.ToString() }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating manual cleaning log");
            return Problem("Failed to create cleaning log");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogWarning("Request to DELETE cleaning log: {LogId}", id);
        try
        {
            await _cleaningLogService.DeleteAsync(id);
            _logger.LogInformation("Deleted cleaning log {LogId} successfully", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cleaning log {LogId}", id);
            return Problem("Error during deletion");
        }
    }

    public class LogCleaningRequest
    {
        public string BinId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public int RemovedKg { get; set; }
        public string? Notes { get; set; }
    }

    [HttpPost("log")]
    public async Task<ActionResult<CleaningLog>> LogCleaning([FromBody] LogCleaningRequest req)
    {
        _logger.LogInformation("Domain Action: Logging cleaning process for Bin: {BinId} by User: {UserId}", req.BinId, req.UserId);

        try
        {
            var created = await _cleaningLogService.LogCleaningAsync(req.BinId, req.UserId, req.RemovedKg, req.Notes);
            _logger.LogInformation("Domain Action Success: Bin {BinId} cleaned, removed {Weight}kg", req.BinId, req.RemovedKg);
            return CreatedAtAction(nameof(Get), new { id = created.Id.ToString() }, created);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Domain Action Failed: Bin {BinId} does not exist. {Message}", req.BinId, ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "CRITICAL: Unexpected error during LogCleaningAsync for Bin {BinId}", req.BinId);
            return Problem("A critical error occurred while processing the cleaning log.");
        }
    }
}
