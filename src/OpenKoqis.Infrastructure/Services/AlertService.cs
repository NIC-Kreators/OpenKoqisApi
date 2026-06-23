using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services;

public class AlertService(IRepository<Alert> repository, ILogger<AlertService> logger) : IAlertService
{
    public async Task<List<Alert>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching all alerts from the database");
        var alerts = await repository.GetAllAsync(cancellationToken);
        logger.LogInformation("Successfully retrieved {Count} alerts", alerts.Count);
        return alerts;
    }

    public async Task<List<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Filtering active (unresolved) alerts");
        var activeAlerts = await repository.AsQueryable()
            .Where(a => !a.IsResolved)
            .ToListAsync(cancellationToken);
        logger.LogInformation("Found {Count} active alerts", activeAlerts.Count);
        return activeAlerts;
    }

    public async Task<List<Alert>> GetByBinIdAsync(string binId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching alerts for BinId: {BinId}", binId);
        var alerts = await repository.AsQueryable()
            .Where(a => a.BinId == binId)
            .ToListAsync(cancellationToken);
        logger.LogInformation("Retrieved {Count} alerts for BinId: {BinId}", alerts.Count, binId);

        return alerts;
    }

    public async Task<Alert> CreateAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating a new alert for BinId: {BinId}, Type: {Type}", alert.BinId, alert.Type);

        if (alert.CreatedAt == default)
        {
            alert.CreatedAt = DateTime.UtcNow;
            logger.LogDebug("Alert CreatedAt timestamp was default, set to UtcNow");
        }

        repository.InsertOne(alert);
        logger.LogInformation("Alert successfully persisted to database with ID: {Id}", alert.Id);

        return alert;
    }

    public async Task ResolveAlertAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to resolve alert with ID: {Id}", id);

        var alert = await repository.FindById(id, cancellationToken);

        if (alert == null)
        {
            logger.LogError("Resolution failed: Alert with ID {Id} not found", id);
            throw new KeyNotFoundException($"Alert with ID {id} not found.");
        }

        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;

        repository.ReplaceOne(alert);
        logger.LogInformation("Alert {Id} status updated to Resolved at {Time}", id, alert.ResolvedAt);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Deleting alert with ID: {Id} from database", id);

        repository.DeleteById(id);
        logger.LogInformation("Alert {Id} has been deleted", id);
    }
}
