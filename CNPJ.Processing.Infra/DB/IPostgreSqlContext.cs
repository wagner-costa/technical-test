using CNPJ.Processing.Infra.Models;
using System.Data;

namespace CNPJ.Processing.Infra.DB
{
    public interface IPostgreSqlContext
    {
        Task<IEnumerable<T>> Query<T>(string query, bool readUncommitted = true);
        Task<IEnumerable<T>> Query<T>(string query, object parameters, bool readUncommitted = true);
        Task<T> QueryFirstOrDefault<T>(string query, bool readUncommitted = true);
        Task<T> QueryFirstOrDefault<T>(string query, object parameters, bool readUncommitted = true);
        Task ExecuteAsync<TEntity>(string query, TEntity entity);
        Task ExecuteAsync(string query, object obj);
        Task<TReturn> ExecuteWithReturnAsync<TEntity, TReturn>(string query, TEntity entity);
        Task<int> ExecuteAsync<T>(string query, object parameters);
        Task<int> ExecuteInBatch<T>(string query, object parameters);
        Task<int> BulkInsert<T>(List<T> rows, string destinationTableName, bool keepIdentity = false);
        Task ExecuteRangeAsync<T>(string query, IEnumerable<T> objetos);
        Task<IEnumerable<T>> AddRangeAsync<T>(string query, IEnumerable<T> objetos);
        IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        void Commit();
        void Rollback();
        Task<PaginatedResult<T>> QueryPaginated<T>(string query, string orderByClause, object parameters, int pageNumber, int pageSize, bool readUncommitted = true);
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }
    }
}
