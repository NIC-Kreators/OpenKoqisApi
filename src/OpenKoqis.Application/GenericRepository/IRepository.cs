using System.Linq.Expressions;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.GenericRepository;

public interface IRepository<TDocument> where TDocument : IEntity
{
    IQueryable<TDocument> AsQueryable();
    void InsertOne(TDocument document, CancellationToken cancellationToken = default);
    void InsertMany(ICollection<TDocument> documents, CancellationToken cancellationToken = default);
    Task<TDocument?> FindById(string id, CancellationToken cancellationToken = default);
    Task<TDocument?> FindOne(Expression<Func<TDocument, bool>> filterExpression, CancellationToken cancellationToken = default);
    void ReplaceOne(TDocument document, CancellationToken cancellationToken = default);
    void DeleteById(string id, CancellationToken cancellationToken = default);
    Task ReplaceOneAsync(TDocument document, CancellationToken cancellationToken = default);
    void DeleteOne(Expression<Func<TDocument, bool>> filterExpression, CancellationToken cancellationToken = default);
    void DeleteMany(Expression<Func<TDocument, bool>> filterExpression, CancellationToken cancellationToken = default);
    Task GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<TDocument>> FindAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);
    Task<List<TDocument>> GetAllAsync(CancellationToken cancellationToken = default);
}
