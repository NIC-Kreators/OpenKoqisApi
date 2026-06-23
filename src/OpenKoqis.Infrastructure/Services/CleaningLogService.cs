using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services;

public class CleaningLogService(
    IRepository<CleaningLog> repo,
    IRepository<Bin> binRepo,
    ILogger<CleaningLogService> logger) : ICleaningLogService
{
    public async Task<List<CleaningLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching all cleaning logs from database");
        var logs = await repo.GetAllAsync(cancellationToken);
        logger.LogInformation("Successfully retrieved {Count} logs", logs.Count);
        return logs;
    }

    public async Task<CleaningLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching for cleaning log with ID: {Id}", id);
        var log = await repo.FindById(id, cancellationToken);

        if (log == null)
            logger.LogWarning("Cleaning log with ID: {Id} was not found", id);
        else
            logger.LogInformation("Found cleaning log for Bin: {BinId}", log.BinId);

        return log;
    }

    public async Task<CleaningLog> CreateAsync(CleaningLog log, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating new manual cleaning log entry");
        log.CreatedAt = DateTime.UtcNow;
        log.UpdatedAt = log.CreatedAt;

        repo.InsertOne(log, cancellationToken);
        logger.LogInformation("Cleaning log inserted with generated ID: {Id}", log.Id);

        return log;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to delete cleaning log: {Id}", id);

        var existing = await repo.FindById(id, cancellationToken);

        if (existing == null)
        {
            logger.LogError("Delete failed: CleaningLog '{Id}' not found", id);
            throw new KeyNotFoundException($"CleaningLog '{id}' not found.");
        }

        repo.DeleteById(id);
        logger.LogInformation("Cleaning log {Id} successfully deleted", id);
    }

    public async Task<CleaningLog> LogCleaningAsync(string binId, string userId, int removedKg, string? notes = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting LogCleaning process for Bin: {BinId} by User: {UserId}", binId, userId);

        var bin = await binRepo.FindById(binId, cancellationToken);

        if (bin == null)
        {
            logger.LogError("LogCleaning failed: Bin {BinId} does not exist", binId);
            throw new KeyNotFoundException($"Bin '{binId}' not found.");
        }

        var cleaning = new CleaningLog
        {
            BinId = ObjectId.Parse(binId),
            UserId = ObjectId.Parse(userId),
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow,
            RemovedWeightKg = removedKg,
            Notes = notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        logger.LogDebug("Inserting cleaning log into database...");
        repo.InsertOne(cleaning, cancellationToken);

        logger.LogInformation("Updating Bin {Id} status to Active after cleaning", binId);
        bin.Status = BinStatus.Active;
        bin.UpdatedAt = DateTime.UtcNow;
        binRepo.ReplaceOne(bin, cancellationToken);

        logger.LogInformation("Cleaning process completed. Recorded {Weight}kg removed", removedKg);

        return cleaning;
    }
}
