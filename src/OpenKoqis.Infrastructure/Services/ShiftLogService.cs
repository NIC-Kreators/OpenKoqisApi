using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services;

public class ShiftLogService(
    IRepository<ShiftLog> repo,
    IRepository<User> userRepo,
    IRepository<Bin> binRepo,
    ILogger<ShiftLogService> logger)
    : IShiftLogService
{
    public async Task<List<ShiftLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching all shift logs from database");
        var logs = await repo.GetAllAsync(cancellationToken);
        logger.LogInformation("Successfully retrieved {Count} shift logs", logs.Count);
        return logs;
    }

    public async Task<ShiftLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching for shift log with ID: {Id}", id);
        var log = await repo.FindById(id, cancellationToken);

        if (log == null)
            logger.LogWarning("Shift log with ID: {Id} was not found", id);
        else
            logger.LogInformation("Shift log with ID: {Id} found", id);

        return log;
    }

    public async Task<ShiftLog> StartShiftAsync(string userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to start a new shift for User: {UserId}", userId);

        var user = await userRepo.FindById(userId, cancellationToken);

        if (user == null)
        {
            logger.LogError("StartShift failed: User with ID {UserId} does not exist", userId);
            throw new KeyNotFoundException($"User '{userId}' not found.");
        }

        var shift = new ShiftLog
        {
            UserId = ObjectId.Parse(userId),
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.MinValue,
            CleanedBins = [],
            DistanceTravelledKm = 0,
            Route = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        repo.InsertOne(shift, cancellationToken);
        logger.LogInformation("New shift started and saved. ShiftId: {ShiftId} for User: {UserId}", shift.Id, userId);

        return shift;
    }

    public async Task EndShiftAsync(string shiftId, DateTime endedAt, IEnumerable<string> cleanedBinIds, double distanceKm, string? route = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to end shift: {ShiftId}", shiftId);

        var shift = await repo.FindById(shiftId, cancellationToken);

        if (shift == null)
        {
            logger.LogError("EndShift failed: Shift with ID {ShiftId} not found", shiftId);
            throw new KeyNotFoundException($"Shift '{shiftId}' not found.");
        }

        List<ObjectId> cleanedObjectIds = [];
        var foundBinsCount = 0;

        foreach (var binId in cleanedBinIds)
        {
            var bin = await binRepo.FindById(binId, cancellationToken);

            if (bin != null)
            {
                cleanedObjectIds.Add(ObjectId.Parse(binId));
                foundBinsCount++;
            }
            else
            {
                logger.LogWarning("Bin with ID {BinId} skipped: not found in database", binId);
            }
        }

        shift.EndedAt = endedAt == default ? DateTime.UtcNow : endedAt;
        shift.CleanedBins = cleanedObjectIds;
        shift.DistanceTravelledKm = distanceKm;
        shift.Route = route ?? shift.Route;
        shift.UpdatedAt = DateTime.UtcNow;

        repo.ReplaceOne(shift, cancellationToken);
        logger.LogInformation("Shift {ShiftId} ended successfully. Bins cleaned: {Count}. Distance: {Distance} km",
                              shiftId, foundBinsCount, distanceKm);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Request to delete shift log: {Id}", id);

        var existing = await repo.FindById(id, cancellationToken);

        if (existing == null)
        {
            logger.LogError("Delete failed: ShiftLog {Id} not found", id);
            throw new KeyNotFoundException($"ShiftLog '{id}' not found.");
        }

        repo.DeleteById(id, cancellationToken);
        logger.LogInformation("Shift log {Id} deleted successfully", id);
    }
}
