namespace OpenKoqis.Application.GenericRepository;

public class MongoCollectionAttribute(string collectionName) : Attribute
{
    public string CollectionName { get; set; } = collectionName;
}
