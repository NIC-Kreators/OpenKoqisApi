using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services
{
    public interface ICleaningLogService
    {
        Task<List<CleaningLog>> GetAllAsync();
        Task<CleaningLog?> GetByIdAsync(string id);
        Task<CleaningLog> CreateAsync(CleaningLog log);
        Task DeleteAsync(string id);

        Task<CleaningLog> LogCleaningAsync(string binId, string userId, int removedKg, string? notes = null);
    }
}
