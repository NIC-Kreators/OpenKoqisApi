using Microsoft.Extensions.Logging;
using OpenKoqis.Application.GenericRepository;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;
using Microsoft.Extensions.Logging; 

namespace OpenKoqis.Infrastructure.Services
{
    public class AlertService : IAlertService
    {
        private readonly IRepository<Alert> _repository;
        private readonly ILogger<AlertService> _logger; 

        public AlertService(IRepository<Alert> repository, ILogger<AlertService> logger)
        {
            _repository = repository;
            _logger = logger;
            _logger.LogInformation("AlertService has been initialized.");
        }

        public async Task<List<Alert>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all alerts from the database.");
            var alerts = await _repository.GetAllAsync();
            _logger.LogInformation("Successfully retrieved {Count} alerts.", alerts.Count);
            return alerts;
        }

        public async Task<List<Alert>> GetActiveAlertsAsync()
        {
            _logger.LogInformation("Filtering active (unresolved) alerts.");
            var activeAlerts = _repository.AsQueryable()
                .Where(a => !a.IsResolved)
                .ToList();
            _logger.LogInformation("Found {Count} active alerts.", activeAlerts.Count);
            return activeAlerts;
        }

        public async Task<List<Alert>> GetByBinIdAsync(string binId)
        {
            _logger.LogInformation("Searching alerts for BinId: {BinId}", binId);
            var alerts = _repository.AsQueryable()
                .Where(a => a.BinId == binId)
                .ToList();
            _logger.LogInformation("Retrieved {Count} alerts for BinId: {BinId}", alerts.Count, binId);
            return alerts;
        }

        public async Task<Alert> CreateAsync(Alert alert)
        {
            _logger.LogInformation("Creating a new alert for BinId: {BinId}, Type: {Type}", alert.BinId, alert.Type);

            if (alert.CreatedAt == default)
            {
                alert.CreatedAt = DateTime.UtcNow;
                _logger.LogDebug("Alert CreatedAt timestamp was default, set to UtcNow.");
            }

            _repository.InsertOne(alert);
            _logger.LogInformation("Alert successfully persisted to database with ID: {Id}", alert.Id);

            return await Task.FromResult(alert);
        }

        public async Task ResolveAlertAsync(string id)
        {
            _logger.LogInformation("Attempting to resolve alert with ID: {Id}", id);

            var alert = await _repository.FindById(id);
            if (alert == null)
            {
                _logger.LogError("Resolution failed: Alert with ID {Id} not found.", id);
                throw new KeyNotFoundException($"Alert with ID {id} not found.");
            }

            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;

            _repository.ReplaceOne(alert);
            _logger.LogInformation("Alert {Id} status updated to Resolved at {Time}.", id, alert.ResolvedAt);

            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            _logger.LogWarning("Deleting alert with ID: {Id} from database.", id);
            _repository.DeleteById(id);
            _logger.LogInformation("Alert {Id} has been deleted.", id);
            await Task.CompletedTask;
        }
    }
}