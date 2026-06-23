using Bogus;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BinsController(
    IBinService binService,
    IAlertService alertService,
    ILogger<BinsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Bin>>> GetAsync([FromQuery] BinStatus? status = null,
        [FromQuery] int? minFillLevel = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching bins with filters: Status={Status}, MinFill={MinFill}", status, minFillLevel);

        var bins = await binService.GetAllAsync(cancellationToken);

        if (status.HasValue)
            bins = bins.Where(b => b.Status == status.Value).ToList();

        if (minFillLevel.HasValue)
            bins = bins.Where(b => b.Telemetry.FillLevel >= minFillLevel.Value).ToList();

        logger.LogDebug("Successfully retrieved {Count} bins after filtering", bins.Count);
        return Ok(bins);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Bin>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting bin details for ID: {BinId}", id);
        var bin = await binService.GetByIdAsync(id, cancellationToken);

        if (bin == null)
        {
            logger.LogWarning("Bin with ID: {BinId} not found", id);
            return NotFound();
        }

        return Ok(bin);
    }

    [HttpPost]
    public async Task<ActionResult<Bin>> PostAsync([FromBody] Bin bin, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating a new bin of type {Type}", bin.Type);

        try
        {
            var created = await binService.CreateAsync(bin, cancellationToken);
            logger.LogInformation("Successfully created bin with ID: {BinId}", created.Id);
            return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id.ToString() }, created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating a new bin");
            return Problem(detail: ex.Message);
        }
    }

    [HttpPost("{id}/telemetry")]
    public async Task<IActionResult> PostTelemetryAsync(string id, [FromBody] BinTelemetry telemetry,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received telemetry update for Bin: {BinId}. FillLevel: {Fill}%", id,
            telemetry.FillLevel);

        try
        {
            telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;

            await binService.UpdateTelemetryAsync(id, telemetry, cancellationToken);
            await binService.UpdateTelemetryHistoryAsync(id, telemetry, cancellationToken);

            if (telemetry.IsSmokeDetected)
            {
                logger.LogCritical("SMOKE DETECTED in Bin: {BinId}!", id);
                await alertService.CreateAsync(
                    new Alert
                    {
                        BinId = id,
                        Type = AlertType.Smoke,
                        Severity = AlertSeverity.Critical,
                        Message = "Danger! Smoke detected in the bin."
                    }, cancellationToken);
            }

            if (telemetry.FillLevel >= 90)
            {
                logger.LogWarning("Bin {BinId} is almost full: {Level}%", id, telemetry.FillLevel);
                await alertService.CreateAsync(
                    new Alert
                    {
                        BinId = id,
                        Type = AlertType.Fullness,
                        Severity = telemetry.FillLevel >= 100 ? AlertSeverity.Critical : AlertSeverity.Warning,
                        Message = $"Container fill level at {telemetry.FillLevel}%"
                    }, cancellationToken);
            }

            return NoContent();
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Telemetry update operation for Bin {BinId} was canceled by the client.", id);
            throw;
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Attempted to update telemetry for non-existent Bin: {BinId}", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update telemetry for Bin: {BinId}", id);
            return Problem(detail: ex.Message);
        }
    }
}
