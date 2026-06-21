using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services;

public interface IShiftLogService
{
    Task<List<ShiftLog>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ShiftLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ShiftLog> StartShiftAsync(string userId, CancellationToken cancellationToken = default);
    Task EndShiftAsync(string shiftId, DateTime endedAt, IEnumerable<string> cleanedBinIds, double distanceKm, string? route = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
