using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.GenericRepository;

public class MongoRepository<TDocument> : IRepository<TDocument> where TDocument : IEntity
{
    private readonly IMongoCollection<TDocument> _collection;
    private readonly IMongoSettings _settings;

    public MongoRepository(IMongoSettings settings)
    {
        var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<TDocument>(MongoRepository<TDocument>.GetCollectionName(typeof(TDocument)));
        _settings = settings;
    }

    private static string GetCollectionName(Type documentType)
    {
        if (documentType.GetCustomAttributes(typeof(MongoCollectionAttribute), true)
                .FirstOrDefault() is MongoCollectionAttribute mongoCollectionAttribute)
        {
            return mongoCollectionAttribute.CollectionName;
        }

        return typeof(TDocument).Name;
    }

    public IQueryable<TDocument> AsQueryable()
    {
        return _collection.AsQueryable();
    }

    // Реализация InsertOne(doc, token)
    public void InsertOne(TDocument document, CancellationToken cancellationToken)
    {
        _collection.InsertOne(document, cancellationToken: cancellationToken);
    }

    // Реализация InsertMany(docs, token)
    public void InsertMany(ICollection<TDocument> documents, CancellationToken cancellationToken)
    {
        _collection.InsertMany(documents, cancellationToken: cancellationToken);
    }

    public async Task<TDocument?> FindById(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<TDocument?> FindById(string id, CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TDocument?> FindOne(Expression<Func<TDocument, bool>> filterExpression)
    {
        return await _collection.Find(filterExpression).FirstOrDefaultAsync();
    }

    public async Task<TDocument?> FindOne(Expression<Func<TDocument, bool>> filterExpression, CancellationToken cancellationToken)
    {
        return await _collection.Find(filterExpression).FirstOrDefaultAsync(cancellationToken);
    }

    // Реализация ReplaceOne(doc, token)
    public void ReplaceOne(TDocument document, CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
        _collection.ReplaceOne(filter, document, cancellationToken: cancellationToken);
    }

    // Реализация ReplaceOneAsync(doc, token)
    public async Task ReplaceOneAsync(TDocument document, CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
        await _collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken);
    }

    // Реализация DeleteById(id, token)
    public void DeleteById(string id, CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        _collection.DeleteOne(filter, cancellationToken);
    }

    // Реализация DeleteOne(filter, token)
    public void DeleteOne(Expression<Func<TDocument, bool>> filterExpression, CancellationToken cancellationToken)
    {
        _collection.DeleteOne(filterExpression, cancellationToken);
    }

    // Реализация DeleteMany(filter, token)
    public void DeleteMany(Expression<Func<TDocument, bool>> filterExpression, CancellationToken cancellationToken)
    {
        _collection.DeleteMany(filterExpression, cancellationToken);
    }

    // Реализация GetByIdAsync(id, token)
    public async Task GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<List<TDocument>> FindAsync(FilterDefinition<TDocument> filter)
    {
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<List<TDocument>> FindAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken)
    {
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<List<TDocument>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<List<TDocument>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _collection.Find(_ => true).ToListAsync(cancellationToken);
    }
}
