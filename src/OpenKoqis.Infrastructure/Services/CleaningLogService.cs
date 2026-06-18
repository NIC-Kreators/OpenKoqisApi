using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services;

public class CleaningLogService : ICleaningLogService
{
    private readonly IRepository<CleaningLog> _repo;
    private readonly IRepository<Bin> _binRepo;
    private readonly ILogger<CleaningLogService> _logger;
    public CleaningLogService(
        IRepository<CleaningLog> repo,
        IRepository<Bin> binRepo,
        ILogger<CleaningLogService> logger)
    {
        _repo = repo;
        _binRepo = binRepo;
        _logger = logger;
    }

    public async Task<List<CleaningLog>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all cleaning logs from database.");
        var logs = await _repo.GetAllAsync();
        _logger.LogInformation("Successfully retrieved {Count} logs.", logs.Count);
        return logs;
    }

    public async Task<CleaningLog?> GetByIdAsync(string id)
    {
        _logger.LogInformation("Searching for cleaning log with ID: {Id}", id);
        var log = await _repo.FindById(id);

        if (log == null)
            _logger.LogWarning("Cleaning log with ID: {Id} was not found.", id);
        else
            _logger.LogInformation("Found cleaning log for Bin: {BinId}", log.BinId);

        return log;
    }

    public async Task<CleaningLog> CreateAsync(CleaningLog log)
    {
        _logger.LogInformation("Creating new manual cleaning log entry.");
        log.CreatedAt = DateTime.UtcNow;
        log.UpdatedAt = log.CreatedAt;

        _repo.InsertOne(log);
        _logger.LogInformation("Cleaning log inserted with generated ID: {Id}", log.Id);

        return await Task.FromResult(log);
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Attempting to delete cleaning log: {Id}", id);

        var existing = await _repo.FindById(id);
        if (existing == null)
        {
            _logger.LogError("Delete failed: CleaningLog '{Id}' not found.", id);
            throw new KeyNotFoundException($"CleaningLog '{id}' not found.");
        }

        _repo.DeleteById(id);
        _logger.LogInformation("Cleaning log {Id} successfully deleted.", id);
        await Task.CompletedTask;
    }

    public async Task<CleaningLog> LogCleaningAsync(string binId, string userId, int removedKg, string? notes = null)
    {
        _logger.LogInformation("Starting LogCleaning process for Bin: {BinId} by User: {UserId}", binId, userId);

        var bin = await _binRepo.FindById(binId);
        if (bin == null)
        {
            _logger.LogError("LogCleaning failed: Bin {BinId} does not exist.", binId);
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

        _logger.LogDebug("Inserting cleaning log into database...");
        _repo.InsertOne(cleaning);

        _logger.LogInformation("Updating Bin {Id} status to Active after cleaning.", binId);
        bin.Status = BinStatus.Active;
        bin.UpdatedAt = DateTime.UtcNow;
        _binRepo.ReplaceOne(bin);

        _logger.LogInformation("Cleaning process completed. Recorded {Weight}kg removed.", removedKg);

        return await Task.FromResult(cleaning);
    }
}
