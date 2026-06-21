using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController(IAlertService alertService, ILogger<AlertsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Alert>>> GetAllAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Request received: GetAll alerts");

        var alerts = await alertService.GetAllAsync(cancellationToken);

        logger.LogInformation("Returning {Count} alerts", alerts.Count);
        return Ok(alerts);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<Alert>>> GetActiveAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Request received: GetActive alerts");

        var alerts = await alertService.GetActiveAlertsAsync(cancellationToken);

        logger.LogInformation("Found {Count} active alerts", alerts.Count);
        return Ok(alerts);
    }

    [HttpGet("bin/{binId}")]
    public async Task<ActionResult<List<Alert>>> GetByBinAsync(string binId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request received: GetByBin for BinId: {BinId}", binId);

        var alerts = await alertService.GetByBinIdAsync(binId, cancellationToken);

        if (alerts.Count == 0)
        {
            logger.LogWarning("No alerts found for BinId: {BinId}", binId);
            return NotFound($"No alerts found for bin with ID {binId}");
        }

        logger.LogInformation("Returning {Count} alerts for BinId: {BinId}", alerts.Count, binId);
        return Ok(alerts);
    }

    [HttpPatch("{id}/resolve")]
    public async Task<IActionResult> ResolveAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to resolve alert with ID: {AlertId}", id);

        try
        {
            await alertService.ResolveAlertAsync(id, cancellationToken);
            logger.LogInformation("Alert {AlertId} successfully resolved", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogError(ex, "Failed to resolve alert: Alert {AlertId} not found", id);
            return NotFound($"Alert with ID {id} not found");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Unexpected error while resolving alert {AlertId}", id);
            return Problem(detail: ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request received: Delete alert with ID: {AlertId}", id);

        try
        {
            await alertService.DeleteAsync(id, cancellationToken);
            logger.LogInformation("Alert {AlertId} deleted successfully", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting alert {AlertId}", id);
            return Problem(detail: ex.Message);
        }
    }
}
