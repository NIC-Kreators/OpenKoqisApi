using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.GenericRepository
{
    public interface IRepository<TDocument> where TDocument : IEntity
    {
        IQueryable<TDocument> AsQueryable();
        void InsertOne(TDocument document);
        void InsertMany(ICollection<TDocument> documents);
        Task<TDocument> FindById(string id);
        Task<TDocument> FindOne(Expression<Func<TDocument, bool>> filterExpression);
        void ReplaceOne(TDocument document);
        void DeleteById(string id);
        void DeleteOne(Expression<Func<TDocument, bool>> filterExpression);
        void DeleteMany(Expression<Func<TDocument, bool>> filterExpression);
        Task GetByIdAsync(string id);
        Task<List<TDocument>> FindAsync(FilterDefinition<TDocument> filter);
        Task<List<TDocument>> GetAllAsync();
    }
}
