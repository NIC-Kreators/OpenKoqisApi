using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services;

public interface IBinService
{
    Task<List<Bin>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bin?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Bin> CreateAsync(Bin bin, CancellationToken cancellationToken = default);
    Task UpdateAsync(string id, Bin bin, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateTelemetryAsync(string binId, BinTelemetry telemetry, CancellationToken cancellationToken = default);
    Task UpdateTelemetryHistoryAsync(string binId, BinTelemetry telemetry, CancellationToken cancellationToken = default);

}
