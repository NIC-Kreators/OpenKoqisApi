using Bogus;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BinsController : ControllerBase
{
    private readonly IBinService _binService;
    private readonly IAlertService _alertService; 
    private readonly ILogger<BinsController> _logger; 

    public BinsController(IBinService binService, IAlertService alertService, ILogger<BinsController> logger)
    {
        _binService = binService;
        _alertService = alertService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Bin>>> Get([FromQuery] BinStatus? status = null, [FromQuery] int? minFillLevel = null)
    {
        _logger.LogInformation("Fetching bins with filters: Status={Status}, MinFill={MinFill}", status, minFillLevel);

        var bins = await _binService.GetAllAsync();

        if (status.HasValue)
            bins = bins.Where(b => b.Status == status.Value).ToList();

        if (minFillLevel.HasValue)
            bins = bins.Where(b => b.Telemetry != null && b.Telemetry.FillLevel >= minFillLevel.Value).ToList();

        _logger.LogDebug("Successfully retrieved {Count} bins after filtering", bins.Count);
        return Ok(bins);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Bin>> Get(string id)
    {
        _logger.LogInformation("Getting bin details for ID: {BinId}", id);
        var bin = await _binService.GetByIdAsync(id);

        if (bin == null)
        {
            _logger.LogWarning("Bin with ID: {BinId} not found", id);
            return NotFound();
        }
        return Ok(bin);
    }

    [HttpPost]
    public async Task<ActionResult<Bin>> Post([FromBody] Bin bin)
    {
        _logger.LogInformation("Creating a new bin of type {Type}", bin.Type);
        try
        {
            var created = await _binService.CreateAsync(bin);
            _logger.LogInformation("Successfully created bin with ID: {BinId}", created.Id);
            return CreatedAtAction(nameof(Get), new { id = created.Id.ToString() }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating a new bin");
            return Problem(detail: ex.Message);
        }
    }

    [HttpPost("{id}/telemetry")]
    public async Task<IActionResult> PostTelemetry(string id, [FromBody] BinTelemetry telemetry)
    {
        _logger.LogInformation("Received telemetry update for Bin: {BinId}. FillLevel: {Fill}%", id, telemetry.FillLevel);
        try
        {
            telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;

            await _binService.UpdateTelemetryAsync(id, telemetry);
            await _binService.UpdateTelemetryHistoryAsync(id, telemetry);

        
            if (telemetry.IsSmokeDetected)
            {
                _logger.LogCritical("SMOKE DETECTED in Bin: {BinId}!", id);
                await _alertService.CreateAsync(new Alert
                {
                    BinId = id,
                    Type = AlertType.Smoke,
                    Severity = AlertSeverity.Critical,
                    Message = "Danger! Smoke detected in the bin."
                });
            }

            if (telemetry.FillLevel >= 90)
            {
                _logger.LogWarning("Bin {BinId} is almost full: {Level}%", id, telemetry.FillLevel);
                await _alertService.CreateAsync(new Alert
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
            _logger.LogWarning("Attempted to update telemetry for non-existent Bin: {BinId}", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update telemetry for Bin: {BinId}", id);
            return Problem(detail: ex.Message);
        }
    }

    [HttpPost("seed/{count}")]
    public async Task<IActionResult> SeedBins(int count = 10)
    {
        _logger.LogInformation("Starting seed operation for {Count} bins in Almaty", count);

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
                f.Address.Longitude(76.80, 77.00),
                f.Address.Latitude(43.20, 43.30)
            }))
            .RuleFor(b => b.Telemetry, f => telemetryFaker.Generate())
            .RuleFor(b => b.TelemetryHistory, f => telemetryFaker.Generate(f.Random.Int(1, 5)).ToArray())
            .RuleFor(b => b.CreatedAt, f => f.Date.Past(1))
            .RuleFor(b => b.UpdatedAt, f => DateTime.UtcNow);

        var fakeBins = binFaker.Generate(count);

        int successCount = 0;
        foreach (var bin in fakeBins)
        {
            await _binService.CreateAsync(bin);
            successCount++;
        }

        _logger.LogInformation("Seed completed. Created {Success} out of {Total} bins", successCount, count);
        return Ok(new { message = $"Successfully seeded {count} bins in Almaty region" });
    }
}