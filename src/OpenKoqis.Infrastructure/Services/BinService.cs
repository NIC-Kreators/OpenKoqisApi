using SmartBin.Application.GenericRepository;
using SmartBin.Domain.Models;
using SmartBin.Application.Services;
using Microsoft.Extensions.Logging; 
using Microsoft.Extensions.Logging;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Infrastructure.Services;

public class BinService : IBinService
{
    private readonly IRepository<Bin> _repository;
    private readonly ILogger<BinService> _logger;

    public BinService(IRepository<Bin> repository, ILogger<BinService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<Bin>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all bins from database.");
        var bins = await _repository.GetAllAsync();
        _logger.LogInformation("Successfully retrieved {Count} bins.", bins.Count);
        return bins;
    }

    public async Task<Bin?> GetByIdAsync(string id)
    {
        _logger.LogInformation("Searching for bin with ID: {BinId}", id);
        var bin = await _repository.FindById(id);

        if (bin == null)
            _logger.LogWarning("Bin with ID: {BinId} was not found.", id);
        else
            _logger.LogInformation("Bin {BinId} found.", id);

        return bin;
    }

    public async Task<Bin> CreateAsync(Bin bin)
    {
        _logger.LogInformation("Creating a new bin of type {BinType}.", bin.Type);

        bin.CreatedAt = DateTime.UtcNow;
        bin.UpdatedAt = bin.CreatedAt;

        _repository.InsertOne(bin);
        _logger.LogInformation("Bin created successfully with ID: {BinId}", bin.Id);

        return await Task.FromResult(bin);
    }

    public async Task UpdateAsync(string id, Bin bin)
    {
        _logger.LogInformation("Attempting to update bin {BinId}.", id);

        var existing = await _repository.FindById(id);
        if (existing == null)
        {
            _logger.LogError("Update failed. Bin '{BinId}' not found.", id);
            throw new KeyNotFoundException($"Bin '{id}' not found.");
        }

        bin.Id = existing.Id;
        bin.CreatedAt = existing.CreatedAt;
        bin.UpdatedAt = DateTime.UtcNow;

        _repository.ReplaceOne(bin);
        _logger.LogInformation("Bin {BinId} updated successfully.", id);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogInformation("Attempting to delete bin {BinId}.", id);

        var existing = await _repository.FindById(id);
        if (existing == null)
        {
            _logger.LogError("Delete failed. Bin '{BinId}' not found.", id);
            throw new KeyNotFoundException($"Bin '{id}' not found.");
        }

        _repository.DeleteById(id);
        _logger.LogInformation("Bin {BinId} deleted from database.", id);
        await Task.CompletedTask;
    }

    public async Task UpdateTelemetryAsync(string binId, BinTelemetry telemetry)
    {
        _logger.LogInformation("Updating current telemetry for bin {BinId}.", binId);

        var existing = await _repository.FindById(binId);
        if (existing == null)
        {
            _logger.LogError("Telemetry update failed. Bin '{BinId}' not found.", binId);
            throw new KeyNotFoundException($"Bin '{binId}' not found.");
        }

        telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;
        existing.Telemetry = telemetry;
        existing.UpdatedAt = DateTime.UtcNow;

        _repository.ReplaceOne(existing);
        _logger.LogInformation("Current telemetry for bin {BinId} updated. Fill level: {FillLevel}%", binId, telemetry.FillLevel);
        await Task.CompletedTask;
    }

    public async Task UpdateTelemetryHistoryAsync(string binId, BinTelemetry telemetry)
    {
        _logger.LogInformation("Adding new entry to telemetry history for bin {BinId}.", binId);

        var existing = await _repository.FindById(binId);
        if (existing == null)
        {
            _logger.LogError("History update failed. Bin '{BinId}' not found.", binId);
            throw new KeyNotFoundException($"Bin '{binId}' not found.");
        }

        telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;

        var historyList = existing.TelemetryHistory?.ToList() ?? new List<BinTelemetry>();
        historyList.Add(telemetry);

        existing.TelemetryHistory = historyList.ToArray();
        existing.UpdatedAt = DateTime.UtcNow;

        _repository.ReplaceOne(existing);
        _logger.LogInformation("History for bin {BinId} updated. Total records: {Count}", binId, existing.TelemetryHistory.Length);
        await Task.CompletedTask;
    }
}