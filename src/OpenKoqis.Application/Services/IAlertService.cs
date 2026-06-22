using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services;

public interface IAlertService
{
    Task<List<Alert>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<List<Alert>> GetByBinIdAsync(string binId, CancellationToken cancellationToken = default);
    Task<Alert> CreateAsync(Alert alert, CancellationToken cancellationToken = default);
    Task ResolveAlertAsync(string id, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
