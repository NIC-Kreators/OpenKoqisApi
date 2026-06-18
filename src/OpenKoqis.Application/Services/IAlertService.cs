using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services
{
    public interface IAlertService
    {
        Task<List<Alert>> GetAllAsync();
        Task<List<Alert>> GetActiveAlertsAsync();
        Task<List<Alert>> GetByBinIdAsync(string binId);
        Task<Alert> CreateAsync(Alert alert);
        Task ResolveAlertAsync(string id);
        Task DeleteAsync(string id);
    }
}
