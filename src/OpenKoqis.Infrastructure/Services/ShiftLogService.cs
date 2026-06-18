using MongoDB.Bson;
using Microsoft.Extensions.Logging; 
using Microsoft.Extensions.Logging;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services;

public class ShiftLogService : IShiftLogService
{
    private readonly IRepository<ShiftLog> _repo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Bin> _binRepo;
    private readonly ILogger<ShiftLogService> _logger; 

    public ShiftLogService(
        IRepository<ShiftLog> repo,
        IRepository<User> userRepo,
        IRepository<Bin> binRepo,
        ILogger<ShiftLogService> logger) 
    {
        _repo = repo;
        _userRepo = userRepo;
        _binRepo = binRepo;
        _logger = logger;
    }

    public async Task<List<ShiftLog>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all shift logs from database.");
        var logs = await _repo.GetAllAsync();
        _logger.LogInformation("Successfully retrieved {Count} shift logs.", logs.Count);
        return logs;
    }

    public async Task<ShiftLog?> GetByIdAsync(string id)
    {
        _logger.LogInformation("Searching for shift log with ID: {Id}", id);
        var log = await _repo.FindById(id);

        if (log == null)
            _logger.LogWarning("Shift log with ID: {Id} was not found.", id);
        else
            _logger.LogInformation("Shift log with ID: {Id} found.", id);

        return log;
    }

    public async Task<ShiftLog> StartShiftAsync(string userId)
    {
        _logger.LogInformation("Attempting to start a new shift for User: {UserId}", userId);

        var user = await _userRepo.FindById(userId);
        if (user == null)
        {
            _logger.LogError("StartShift failed: User with ID {UserId} does not exist.", userId);
            throw new KeyNotFoundException($"User '{userId}' not found.");
        }

        var shift = new ShiftLog
        {
            UserId = ObjectId.Parse(userId),
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.MinValue,
            CleanedBins = new List<ObjectId>(),
            DistanceTravelledKm = 0,
            Route = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repo.InsertOne(shift);
        _logger.LogInformation("New shift started and saved. ShiftId: {ShiftId} for User: {UserId}", shift.Id, userId);

        return await Task.FromResult(shift);
    }

    public async Task EndShiftAsync(string shiftId, DateTime endedAt, IEnumerable<string> cleanedBinIds, double distanceKm, string? route = null)
    {
        _logger.LogInformation("Attempting to end shift: {ShiftId}", shiftId);

        var shift = await _repo.FindById(shiftId);
        if (shift == null)
        {
            _logger.LogError("EndShift failed: Shift with ID {ShiftId} not found.", shiftId);
            throw new KeyNotFoundException($"Shift '{shiftId}' not found.");
        }

        var cleanedObjectIds = new List<ObjectId>();
        int foundBinsCount = 0;

        foreach (var binId in cleanedBinIds ?? Enumerable.Empty<string>())
        {
            var bin = await _binRepo.FindById(binId);
            if (bin != null)
            {
                cleanedObjectIds.Add(ObjectId.Parse(binId));
                foundBinsCount++;
            }
            else
            {
                _logger.LogWarning("Bin with ID {BinId} skipped: not found in database.", binId);
            }
        }

        shift.EndedAt = endedAt == default ? DateTime.UtcNow : endedAt;
        shift.CleanedBins = cleanedObjectIds;
        shift.DistanceTravelledKm = distanceKm;
        shift.Route = route ?? shift.Route;
        shift.UpdatedAt = DateTime.UtcNow;

        _repo.ReplaceOne(shift);
        _logger.LogInformation("Shift {ShiftId} ended successfully. Bins cleaned: {Count}. Distance: {Distance} km",
            shiftId, foundBinsCount, distanceKm);

        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Request to delete shift log: {Id}", id);

        var existing = await _repo.FindById(id);
        if (existing == null)
        {
            _logger.LogError("Delete failed: ShiftLog {Id} not found.", id);
            throw new KeyNotFoundException($"ShiftLog '{id}' not found.");
        }

        _repo.DeleteById(id);
        _logger.LogInformation("Shift log {Id} deleted successfully.", id);

        await Task.CompletedTask;
    }
}