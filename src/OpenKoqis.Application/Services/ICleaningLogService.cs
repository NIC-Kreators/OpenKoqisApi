using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services;

public interface ICleaningLogService
{
    Task<List<CleaningLog>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CleaningLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<CleaningLog> CreateAsync(CleaningLog log, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<CleaningLog> LogCleaningAsync(string binId, string userId, int removedKg, string? notes = null, CancellationToken cancellationToken = default);
}
