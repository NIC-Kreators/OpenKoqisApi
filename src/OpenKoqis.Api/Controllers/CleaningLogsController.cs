using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleaningLogsController(
    ICleaningLogService cleaningLogService,
    ILogger<CleaningLogsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CleaningLog>>> GetAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching all cleaning logs at {Time}", DateTime.UtcNow);
        var logs = await cleaningLogService.GetAllAsync(cancellationToken);
        logger.LogDebug("Retrieved {Count} logs from database", logs.Count);
        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CleaningLog>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Requested cleaning log with ID: {LogId}", id);
        var log = await cleaningLogService.GetByIdAsync(id, cancellationToken);

        if (log == null)
        {
            logger.LogWarning("Cleaning log {LogId} not found", id);
            return NotFound();
        }

        return Ok(log);
    }

    [HttpPost]
    public async Task<ActionResult<CleaningLog>> PostAsync([FromBody] CleaningLog log, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating a new manual cleaning log entry");

        try
        {
            var created = await cleaningLogService.CreateAsync(log, cancellationToken);
            logger.LogInformation("Successfully created log with ID: {LogId}", created.Id);
            return CreatedAtAction(nameof(GetByIdAsync), new
            {
                id = created.Id
            }, created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating manual cleaning log");
            return Problem("Failed to create cleaning log");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Request to DELETE cleaning log: {LogId}", id);

        try
        {
            await cleaningLogService.DeleteAsync(id,  cancellationToken);
            logger.LogInformation("Deleted cleaning log {LogId} successfully", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting cleaning log {LogId}", id);
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
    public async Task<ActionResult<CleaningLog>> LogCleaningAsync([FromBody] LogCleaningRequest req, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Domain Action: Logging cleaning process for Bin: {BinId} by User: {UserId}", req.BinId, req.UserId);

        try
        {
            var created = await cleaningLogService.LogCleaningAsync(req.BinId, req.UserId, req.RemovedKg, req.Notes, cancellationToken);
            logger.LogInformation("Domain Action Success: Bin {BinId} cleaned, removed {Weight}kg", req.BinId, req.RemovedKg);
            return CreatedAtAction(nameof(GetByIdAsync), new
            {
                id = created.Id
            }, created);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning("Domain Action Failed: Bin {BinId} does not exist. {Message}", req.BinId, ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "CRITICAL: Unexpected error during LogCleaningAsync for Bin {BinId}", req.BinId);
            return Problem("A critical error occurred while processing the cleaning log.");
        }
    }
}
