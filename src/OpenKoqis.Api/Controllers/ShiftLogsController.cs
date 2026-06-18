using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftLogsController : ControllerBase
{
    private readonly IShiftLogService _shiftLogService;
    private readonly ILogger<ShiftLogsController> _logger; // ��������� ������

    public ShiftLogsController(IShiftLogService shiftLogService, ILogger<ShiftLogsController> logger)
    {
        _shiftLogService = shiftLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ShiftLog>>> Get()
    {
        _logger.LogInformation("Request received: Get all shift logs.");

        var shifts = await _shiftLogService.GetAllAsync();

        _logger.LogInformation("Successfully retrieved {Count} shifts.", shifts.Count);
        return Ok(shifts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShiftLog>> Get(string id)
    {
        _logger.LogInformation("Request received: Get shift log with ID: {Id}", id);

        var shift = await _shiftLogService.GetByIdAsync(id);

        if (shift == null)
        {
            _logger.LogWarning("Shift log with ID: {Id} was not found.", id);
            return NotFound();
        }

        _logger.LogInformation("Successfully retrieved shift log for User: {UserId}", shift.UserId);
        return Ok(shift);
    }

    public class StartShiftRequest
    {
        public string UserId { get; set; } = null!;
    }

    [HttpPost("start")]
    public async Task<ActionResult<ShiftLog>> Start([FromBody] StartShiftRequest req)
    {
        _logger.LogInformation("Attempting to start a new shift for User: {UserId}", req.UserId);

        try
        {
            var created = await _shiftLogService.StartShiftAsync(req.UserId);
            _logger.LogInformation("Shift started successfully. Assigned ID: {ShiftId}", created.Id);
            return CreatedAtAction(nameof(Get), new { id = created.Id.ToString() }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start shift for User: {UserId}", req.UserId);
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
    public async Task<IActionResult> End(string id, [FromBody] EndShiftRequest req)
    {
        _logger.LogInformation("Attempting to end shift ID: {Id}. Distance: {Distance}km", id, req.DistanceKm);

        try
        {
            await _shiftLogService.EndShiftAsync(
                id,
                req.EndedAt ?? default,
                req.CleanedBinIds ?? Enumerable.Empty<string>(),
                req.DistanceKm,
                req.Route);

            _logger.LogInformation("Shift ID: {Id} ended successfully.", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while ending shift ID: {Id}", id);
            return Problem(detail: ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Request to delete shift log ID: {Id}", id);

        await _shiftLogService.DeleteAsync(id);

        _logger.LogInformation("Shift log ID: {Id} deleted (if it existed).", id);
        return NoContent();
    }
}