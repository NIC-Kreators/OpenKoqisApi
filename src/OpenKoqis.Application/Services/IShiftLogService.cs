using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services;

public interface IShiftLogService
{
    Task<List<ShiftLog>> GetAllAsync();
    Task<ShiftLog?> GetByIdAsync(string id);
    Task<ShiftLog> StartShiftAsync(string userId);
    Task EndShiftAsync(string shiftId, DateTime endedAt, IEnumerable<string> cleanedBinIds, double distanceKm, string? route = null);
    Task DeleteAsync(string id);
}
