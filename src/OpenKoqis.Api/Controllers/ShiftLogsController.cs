using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftLogsController(IShiftLogService shiftLogService, ILogger<ShiftLogsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ShiftLog>>> GetAsync()
    {
        logger.LogInformation("Request received: Get all shift logs");

        var shifts = await shiftLogService.GetAllAsync();

        logger.LogInformation("Successfully retrieved {Count} shifts", shifts.Count);
        return Ok(shifts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShiftLog>> GetByIdAsync(string id)
    {
        logger.LogInformation("Request received: Get shift log with ID: {Id}", id);

        var shift = await shiftLogService.GetByIdAsync(id);

        if (shift == null)
        {
            logger.LogWarning("Shift log with ID: {Id} was not found", id);
            return NotFound();
        }

        logger.LogInformation("Successfully retrieved shift log for User: {UserId}", shift.UserId);
        return Ok(shift);
    }

    public class StartShiftRequest
    {
        public string UserId { get; set; } = null!;
    }

    [HttpPost("start")]
    public async Task<ActionResult<ShiftLog>> StartAsync([FromBody] StartShiftRequest req)
    {
        logger.LogInformation("Attempting to start a new shift for User: {UserId}", req.UserId);

        try
        {
            var created = await shiftLogService.StartShiftAsync(req.UserId);
            logger.LogInformation("Shift started successfully. Assigned ID: {ShiftId}", created.Id);
            return CreatedAtAction(nameof(GetByIdAsync), new
            {
                id = created.Id
            }, created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start shift for User: {UserId}", req.UserId);
            return Problem(detail: ex.Message);
        }
    }

    public class EndShiftRequest
    {
        public DateTime? EndedAt { get; set; }
        public IEnumerable<string>? CleanedBinIds { get; set; }
        public double DistanceKm { get; set; }
        public string? Route { get; set; }
    }

    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndAsync(string id, [FromBody] EndShiftRequest req)
    {
        logger.LogInformation("Attempting to end shift ID: {Id}. Distance: {Distance}km", id, req.DistanceKm);

        try
        {
            await shiftLogService.EndShiftAsync(
                id,
                req.EndedAt ?? default,
                req.CleanedBinIds ?? Enumerable.Empty<string>(),
                req.DistanceKm,
                req.Route);

            logger.LogInformation("Shift ID: {Id} ended successfully", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while ending shift ID: {Id}", id);
            return Problem(detail: ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        logger.LogInformation("Request to delete shift log ID: {Id}", id);

        await shiftLogService.DeleteAsync(id);

        logger.LogInformation("Shift log ID: {Id} deleted (if it existed)", id);
        return NoContent();
    }
}
