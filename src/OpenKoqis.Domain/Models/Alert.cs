using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models
{
    public enum AlertSeverity
    {
        Info,      // Информация
        Warning,   // Предупреждение (например, заполнено на 80%)
        Critical   // Критично (дым, перегруз или 100% заполнение)
    }

    public enum AlertType
    {
        Smoke,        // Задымление
        Overload,     // Перегруз
        Fullness,     // Переполнение
        ConnectionLost // Потеря связи с датчиком
    }

    public class Alert : IEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // Магия здесь!
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string BinId { get; set; } = null!; // ID контейнера, где произошла аномалия

        public AlertType Type { get; set; }

        public AlertSeverity Severity { get; set; }

        public string Message { get; set; } = null!; // Описание (напр. "Обнаружен дым!")

        public string? ValueAtTime { get; set; } // Значение датчика в момент аномалии

        public bool IsResolved { get; set; } = false; // Решена ли проблема

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}