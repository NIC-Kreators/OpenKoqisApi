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
    public async Task<ActionResult<List<Bin>>> GetAsync([FromQuery] BinStatus? status = null, [FromQuery] int? minFillLevel = null)
    {
        logger.LogInformation("Fetching bins with filters: Status={Status}, MinFill={MinFill}", status, minFillLevel);

        var bins = await binService.GetAllAsync();

        if (status.HasValue)
            bins = bins.Where(b => b.Status == status.Value).ToList();

        if (minFillLevel.HasValue)
            bins = bins.Where(b => b.Telemetry.FillLevel >= minFillLevel.Value).ToList();

        logger.LogDebug("Successfully retrieved {Count} bins after filtering", bins.Count);
        return Ok(bins);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Bin>> GetByIdAsync(string id)
    {
        logger.LogInformation("Getting bin details for ID: {BinId}", id);
        var bin = await binService.GetByIdAsync(id);

        if (bin == null)
        {
            logger.LogWarning("Bin with ID: {BinId} not found", id);
            return NotFound();
        }

        return Ok(bin);
    }

    [HttpPost]
    public async Task<ActionResult<Bin>> PostAsync([FromBody] Bin bin)
    {
        logger.LogInformation("Creating a new bin of type {Type}", bin.Type);

        try
        {
            var created = await binService.CreateAsync(bin);
            logger.LogInformation("Successfully created bin with ID: {BinId}", created.Id);
            return CreatedAtAction(nameof(GetByIdAsync), new
            {
                id = created.Id.ToString()
            }, created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating a new bin");
            return Problem(detail: ex.Message);
        }
    }

    [HttpPost("{id}/telemetry")]
    public async Task<IActionResult> PostTelemetryAsync(string id, [FromBody] BinTelemetry telemetry)
    {
        logger.LogInformation("Received telemetry update for Bin: {BinId}. FillLevel: {Fill}%", id, telemetry.FillLevel);

        try
        {
            telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;

            await binService.UpdateTelemetryAsync(id, telemetry);
            await binService.UpdateTelemetryHistoryAsync(id, telemetry);


            if (telemetry.IsSmokeDetected)
            {
                logger.LogCritical("SMOKE DETECTED in Bin: {BinId}!", id);
                await alertService.CreateAsync(new Alert
                {
                    BinId = id,
                    Type = AlertType.Smoke,
                    Severity = AlertSeverity.Critical,
                    Message = "Danger! Smoke detected in the bin."
                });
            }

            if (telemetry.FillLevel >= 90)
            {
                logger.LogWarning("Bin {BinId} is almost full: {Level}%", id, telemetry.FillLevel);
                await alertService.CreateAsync(new Alert
                {
                    BinId = id,
                    Type = AlertType.Fullness,
                    Severity = telemetry.FillLevel >= 100 ? AlertSeverity.Critical : AlertSeverity.Warning,
                    Message = $"Container fill level at {telemetry.FillLevel}%"
                });
            }

            return NoContent();
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

    [HttpPost("seed/{count}")]
    public async Task<IActionResult> SeedBinsAsync(int count = 10)
    {
        logger.LogInformation("Starting seed operation for {Count} bins in Almaty", count);

        var telemetryFaker = new Faker<BinTelemetry>()
            .RuleFor(t => t.FillLevel, f => f.Random.Int(0, 100))
            .RuleFor(t => t.IsSmokeDetected, f => f.Random.Bool(0.05f))
            .RuleFor(t => t.IsOverloaded, f => f.Random.Bool(0.1f))
            .RuleFor(t => t.LastUpdated, f => f.Date.Recent(1));

        var binFaker = new Faker<Bin>()
            .RuleFor(b => b.Type, f => f.PickRandom<BinType>())
            .RuleFor(b => b.Status, f => f.PickRandom<BinStatus>())
            .RuleFor(b => b.Location, f => new GeoPoint(new double[]
            {
                f.Address.Longitude(76.80, 77.00), f.Address.Latitude(43.20, 43.30)
            }))
            .RuleFor(b => b.Telemetry, f => telemetryFaker.Generate())
            .RuleFor(b => b.TelemetryHistory, f => telemetryFaker.Generate(f.Random.Int(1, 5)).ToArray())
            .RuleFor(b => b.CreatedAt, f => f.Date.Past(1))
            .RuleFor(b => b.UpdatedAt, f => DateTime.UtcNow);

        var fakeBins = binFaker.Generate(count);

        int successCount = 0;

        foreach (var bin in fakeBins)
        {
            await binService.CreateAsync(bin);
            successCount++;
        }

        logger.LogInformation("Seed completed. Created {Success} out of {Total} bins", successCount, count);
        return Ok(new
        {
            message = $"Successfully seeded {count} bins in Almaty region"
        });
    }
}
