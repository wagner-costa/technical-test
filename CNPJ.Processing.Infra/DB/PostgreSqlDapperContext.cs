using CNPJ.Processing.Infra.Converters;
using CNPJ.Processing.Infra.Models;
using Dapper;
using Npgsql;
using Polly;
using Polly.Retry;
using Serilog;
using System.Data;

namespace CNPJ.Processing.Infra.DB
{
    public class PostgreSqlDapperContext : IPostgreSqlContext, IDisposable
    {
        private readonly string _postgreSqlConnection;
        private IDbConnection _connection;
        private IDbTransaction? _transaction;
        private readonly AsyncRetryPolicy _asyncRetryPolicy = Policy.Handle<NpgsqlException>()
                                                                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, numberRetry) =>
                                                                    {
                                                                        Log.Warning(ex, "Tentativa de conexão falhou. Tentativa {RetryAttempt}", numberRetry);
                                                                    });

        public PostgreSqlDapperContext(string postgreSqlConnection)
        {
            _postgreSqlConnection = postgreSqlConnection;
            HandleConnection();
        }

        public async Task<IEnumerable<T>> Query<T>(string query, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(() => _connection.QueryAsync<T>(query, transaction: _transaction));
        }

        public async Task<IEnumerable<T>> Query<T>(string query, object parameters, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(() => _connection.QueryAsync<T>(query, parameters, _transaction));
        }

        public async Task<T> QueryFirstOrDefault<T>(string query, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(() => _connection.QueryFirstOrDefaultAsync<T>(query, transaction: _transaction));
        }

        public async Task<T> QueryFirstOrDefault<T>(string query, object parameters, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(() => _connection.QueryFirstOrDefaultAsync<T>(query, parameters, transaction: _transaction));
        }

        public async Task ExecuteAsync<TEntity>(string query, TEntity entity)
        {
            HandleConnection();
            await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                var idResponse = await _connection.ExecuteAsync(query, entity, _transaction);
                if (idResponse == 0)
                    throw new Exception("Zero rows affected.");
            });
        }

        public async Task ExecuteAsync(string query, object obj)
        {
            HandleConnection();
            await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                var idResponse = await _connection.ExecuteAsync(query, obj, _transaction);
                if (idResponse == 0)
                    throw new Exception("Zero rows affected.");
            });
        }

        public async Task<TReturn> ExecuteWithReturnAsync<TEntity, TReturn>(string query, TEntity entity)
        {
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(() => _connection.ExecuteScalarAsync<TReturn>(query, entity, _transaction));
        }

        public async Task<int> ExecuteAsync<T>(string query, object parameters)
        {
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(() => _connection.ExecuteAsync(query, parameters, _transaction));
        }

        public async Task<int> ExecuteInBatch<T>(string query, object parameters)
        {
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(() => _connection.ExecuteAsync(query, parameters, _transaction));
        }

        public async Task<int> BulkInsert<T>(List<T> rows, string destinationTableName, bool keepIdentity = false)
        {
            HandleConnection();

            // PostgreSQL não tem um SqlBulkCopy nativo como SQL Server
            // Vamos implementar usando a extensão Npgsql COPY

            var npgsqlConn = (NpgsqlConnection)_connection;
            var dataTable = ListtoDataTableConverter.ToDataTable(rows);
            int rowsAffected = 0;

            // Obter nomes das colunas da tabela
            var columnNames = new List<string>();
            foreach (DataColumn column in dataTable.Columns)
            {
                columnNames.Add($"\"{column.ColumnName}\"");
            }

            // Construir comando COPY
            string copyCommand = $"COPY {destinationTableName} ({string.Join(", ", columnNames)}) FROM STDIN (FORMAT BINARY)";

            using (var writer = npgsqlConn.BeginBinaryImport(copyCommand))
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    writer.StartRow();

                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        if (row[i] == DBNull.Value)
                            writer.WriteNull();
                        else
                            writer.Write(row[i]);
                    }

                    rowsAffected++;
                }

                writer.Complete();
            }

            return await Task.FromResult(rowsAffected);
        }

        public async Task ExecuteRangeAsync<T>(string query, IEnumerable<T> objetos)
        {
            HandleConnection();
            BeginTransaction();

            try
            {
                foreach (var objeto in objetos)
                    await _connection.ExecuteScalarAsync(query, objeto, _transaction);
                Commit();
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<T>> AddRangeAsync<T>(string query, IEnumerable<T> objetos)
        {
            HandleConnection();
            BeginTransaction();

            try
            {
                foreach (var objeto in objetos)
                {
                    // Modificar a consulta para PostgreSQL - usar RETURNING ao invés de SCOPE_IDENTITY()
                    var modifiedQuery = $"{query} RETURNING id";
                    int id = await _connection.ExecuteScalarAsync<int>(modifiedQuery, objeto, _transaction);
                    typeof(T).GetProperty("Id")?.SetValue(objeto, id);
                }
                Commit();
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }

            return objetos;
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _transaction = _connection.BeginTransaction(isolationLevel);
            return _transaction;
        }

        public void Commit()
        {
            if (_transaction == null)
                throw new Exception("Nao é possível realizar commit sem abrir uma transaction");
            _transaction.Commit();
            _transaction = null;
        }

        public void Rollback()
        {
            if (_transaction == null)
                throw new Exception("Nao é possível realizar rollback sem abrir uma transaction");
            _transaction.Rollback();
            _transaction = null;
        }

        public async Task<PaginatedResult<T>> QueryPaginated<T>(string query, string orderByClause, object parameters, int pageNumber, int pageSize, bool readUncommitted = true)
        {
            var offset = (pageNumber - 1) * pageSize;

            // Modificar a sintaxe para PostgreSQL
            var paginatedQuery = $"{query} {orderByClause} LIMIT @PageSize OFFSET @Offset";
            var countQuery = $"SELECT COUNT(*) FROM ({query}) AS CountQuery";

            var countParameters = new DynamicParameters(parameters);
            var paginatedParameters = new DynamicParameters(parameters);
            paginatedParameters.Add("Offset", offset);
            paginatedParameters.Add("PageSize", pageSize);

            if (readUncommitted)
            {
                countQuery = HandleReadUncommitted(countQuery, readUncommitted);
                paginatedQuery = HandleReadUncommitted(paginatedQuery, readUncommitted);
            }

            HandleConnection();
            var totalCount = await _asyncRetryPolicy.ExecuteAsync(() => _connection.ExecuteScalarAsync<int>(countQuery, countParameters));
            var items = await _asyncRetryPolicy.ExecuteAsync(() => _connection.QueryAsync<T>(paginatedQuery, paginatedParameters));

            return new PaginatedResult<T>(items, totalCount, pageNumber, pageSize);
        }

        public IDbConnection Connection => _connection;
        public IDbTransaction? Transaction => _transaction;

        public void Dispose()
        {
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }

        private void HandleConnection()
        {
            var retryPolicyDBOpen = Policy.Handle<NpgsqlException>()
                                          .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, numberRetry) =>
                                          {
                                              Log.Warning(ex, "Tentativa de conexão falhou. Tentativa {RetryAttempt}", numberRetry);
                                          });

            if (_connection == null)
                _connection = new NpgsqlConnection(_postgreSqlConnection);

            retryPolicyDBOpen.Execute(() =>
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();
            });
        }

        private string HandleReadUncommitted(string query, bool readUncommitted)
        {
            return query;
        }
    }
}