using Microsoft.Extensions.Logging;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services;

public class BinService(IRepository<Bin> repository, ILogger<BinService> logger) : IBinService
{
    public async Task<List<Bin>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching all bins from database");
        var bins = await repository.GetAllAsync(cancellationToken);
        logger.LogInformation("Successfully retrieved {Count} bins", bins.Count);
        return bins;
    }

    public async Task<Bin?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching for bin with ID: {BinId}", id);
        var bin = await repository.FindById(id, cancellationToken);

        if (bin == null)
            logger.LogWarning("Bin with ID: {BinId} was not found", id);
        else
            logger.LogInformation("Bin {BinId} found", id);

        return bin;
    }

    public async Task<Bin> CreateAsync(Bin bin, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating a new bin of type {BinType}", bin.Type);

        bin.CreatedAt = DateTime.UtcNow;
        bin.UpdatedAt = bin.CreatedAt;

        repository.InsertOne(bin);
        logger.LogInformation("Bin created successfully with ID: {BinId}", bin.Id);

        return bin;
    }

    public async Task UpdateAsync(string id, Bin bin, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to update bin {BinId}", id);

        var existing = await repository.FindById(id, cancellationToken);

        if (existing == null)
        {
            logger.LogError("Update failed. Bin '{BinId}' not found", id);
            throw new KeyNotFoundException($"Bin '{id}' not found.");
        }

        bin.Id = existing.Id;
        bin.CreatedAt = existing.CreatedAt;
        bin.UpdatedAt = DateTime.UtcNow;

        repository.ReplaceOne(bin);
        logger.LogInformation("Bin {BinId} updated successfully", id);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to delete bin {BinId}", id);

        var existing = await repository.FindById(id, cancellationToken);

        if (existing == null)
        {
            logger.LogError("Delete failed. Bin '{BinId}' not found", id);
            throw new KeyNotFoundException($"Bin '{id}' not found.");
        }

        repository.DeleteById(id);
        logger.LogInformation("Bin {BinId} deleted from database", id);
    }

    public async Task UpdateTelemetryAsync(string binId, BinTelemetry telemetry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating current telemetry for bin {BinId}", binId);

        var existing = await repository.FindById(binId, cancellationToken);

        if (existing == null)
        {
            logger.LogError("Telemetry update failed. Bin '{BinId}' not found", binId);
            throw new KeyNotFoundException($"Bin '{binId}' not found.");
        }

        telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;
        existing.Telemetry = telemetry;
        existing.UpdatedAt = DateTime.UtcNow;

        repository.ReplaceOne(existing);
        logger.LogInformation("Current telemetry for bin {BinId} updated. Fill level: {FillLevel}%", binId, telemetry.FillLevel);
    }

    public async Task UpdateTelemetryHistoryAsync(string binId, BinTelemetry telemetry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding new entry to telemetry history for bin {BinId}", binId);

        var existing = await repository.FindById(binId, cancellationToken);

        if (existing == null)
        {
            logger.LogError("History update failed. Bin '{BinId}' not found", binId);
            throw new KeyNotFoundException($"Bin '{binId}' not found.");
        }

        telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;

        var historyList = existing.TelemetryHistory?.ToList() ?? [];
        historyList.Add(telemetry);

        existing.TelemetryHistory = [..historyList];
        existing.UpdatedAt = DateTime.UtcNow;

        repository.ReplaceOne(existing);
        logger.LogInformation("History for bin {BinId} updated. Total records: {Count}", binId, existing.TelemetryHistory.Length);
    }
}
