using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models
{
    public interface IEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // Позволяет работать со строками как с ObjectId
        string Id { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }
}
