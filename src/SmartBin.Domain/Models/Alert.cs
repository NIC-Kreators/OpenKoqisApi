using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBin.Domain.Models
{
    public enum AlertSeverity
    {
        Info,      // Information
        Warning,   // Warning (e.g., 80% full)
        Critical   // Critical (smoke, overload, or 100% full)
    }

    public enum AlertType
    {
        Smoke,          // Smoke
        Overload,       // Overload
        Fullness,       // Overflow
        ConnectionLost  // Disconnected
    }

    public class Alert : IEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] 
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string BinId { get; set; } = null!; // contained ID where anomaly occured

        public AlertType Type { get; set; }

        public AlertSeverity Severity { get; set; }

        public string Message { get; set; } = null!; // Message description

        public string? ValueAtTime { get; set; } // Detector value when anomaly occured
        

        public bool IsResolved { get; set; } = false; // Is problem fixed.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}