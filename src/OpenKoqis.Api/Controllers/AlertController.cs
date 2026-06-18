using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger; // Добавляем логгер

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Alert>>> GetAll()
    {
        _logger.LogInformation("Request received: GetAll alerts.");

        var alerts = await _alertService.GetAllAsync();

        _logger.LogInformation("Returning {Count} alerts.", alerts.Count);
        return Ok(alerts);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<Alert>>> GetActive()
    {
        _logger.LogInformation("Request received: GetActive alerts.");

        var alerts = await _alertService.GetActiveAlertsAsync();

        _logger.LogInformation("Found {Count} active alerts.", alerts.Count);
        return Ok(alerts);
    }

    [HttpGet("bin/{binId}")]
    public async Task<ActionResult<List<Alert>>> GetByBin(string binId)
    {
        _logger.LogInformation("Request received: GetByBin for BinId: {BinId}", binId);

        var alerts = await _alertService.GetByBinIdAsync(binId);

        if (alerts == null || !alerts.Any())
        {
            _logger.LogWarning("No alerts found for BinId: {BinId}", binId);
            return NotFound($"No alerts found for bin with ID {binId}");
        }

        _logger.LogInformation("Returning {Count} alerts for BinId: {BinId}", alerts.Count, binId);
        return Ok(alerts);
    }

    [HttpPatch("{id}/resolve")]
    public async Task<IActionResult> Resolve(string id)
    {
        _logger.LogInformation("Attempting to resolve alert with ID: {AlertId}", id);

        try
        {
            await _alertService.ResolveAlertAsync(id);
            _logger.LogInformation("Alert {AlertId} successfully resolved.", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Failed to resolve alert: Alert {AlertId} not found.", id);
            return NotFound($"Alert with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error while resolving alert {AlertId}.", id);
            return Problem(detail: ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Request received: Delete alert with ID: {AlertId}", id);

        try
        {
            await _alertService.DeleteAsync(id);
            _logger.LogInformation("Alert {AlertId} deleted successfully.", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting alert {AlertId}.", id);
            return Problem(detail: ex.Message);
        }
    }
}