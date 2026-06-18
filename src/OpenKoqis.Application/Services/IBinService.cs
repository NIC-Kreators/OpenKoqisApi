using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services;

public interface IBinService
{
    Task<List<Bin>> GetAllAsync();
    Task<Bin?> GetByIdAsync(string id);
    Task<Bin> CreateAsync(Bin bin);
    Task UpdateAsync(string id, Bin bin);
    Task DeleteAsync(string id);
    Task UpdateTelemetryAsync(string binId, BinTelemetry telemetry);
    Task UpdateTelemetryHistoryAsync(string binId, BinTelemetry telemetry);

}
