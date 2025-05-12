using CNPJ.Processing.Infra.Context;
using CNPJ.Processing.Infra.Converters;
using CNPJ.Processing.Infra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Polly;
using Polly.Retry;
using Serilog;
using System.Data;

namespace CNPJ.Processing.Infra.DB
{
    public class PostgreSqlEFContext : IPostgreSqlContext, IDisposable
    {
        private readonly string _postgreSqlConnection;
        private readonly DbContext _dbContext;
        private IDbConnection _connection;
        private IDbTransaction? _transaction;
        private readonly AsyncRetryPolicy _asyncRetryPolicy = Policy.Handle<NpgsqlException>()
                                                                   .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, numberRetry) =>
                                                                   {
                                                                       Log.Warning(ex, "Tentativa de conexão falhou. Tentativa {RetryAttempt}", numberRetry);
                                                                   });

        public PostgreSqlEFContext(string postgreSqlConnection)
        {
            _postgreSqlConnection = postgreSqlConnection;

            // Criar DbContext com a string de conexão
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseNpgsql(_postgreSqlConnection);
            _dbContext = new ApplicationDbContext(optionsBuilder.Options);

            HandleConnection();
        }

        public async Task<IEnumerable<T>> Query<T>(string query, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                // Formatar para PostgreSQL se necessário
                query = FormatSqlForPostgres(query);
                return await _dbContext.Database.SqlQueryRaw<T>(query).ToListAsync();
            });
        }

        public async Task<IEnumerable<T>> Query<T>(string query, object parameters, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var npgsqlParameters = CreateNpgsqlParameters(parameters);
                return await _dbContext.Database.SqlQueryRaw<T>(query, npgsqlParameters).ToListAsync();
            });
        }

        public async Task<T> QueryFirstOrDefault<T>(string query, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var result = await _dbContext.Database.SqlQueryRaw<T>(query).ToListAsync();
                return result.FirstOrDefault();
            });
        }

        public async Task<T> QueryFirstOrDefault<T>(string query, object parameters, bool readUncommitted = true)
        {
            query = HandleReadUncommitted(query, readUncommitted);
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var npgsqlParameters = CreateNpgsqlParameters(parameters);
                var result = await _dbContext.Database.SqlQueryRaw<T>(query, npgsqlParameters).ToListAsync();
                return result.FirstOrDefault();
            });
        }

        public async Task ExecuteAsync<TEntity>(string query, TEntity entity)
        {
            HandleConnection();
            await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var npgsqlParameters = CreateNpgsqlParameters(entity);
                var idResponse = await _dbContext.Database.ExecuteSqlRawAsync(query, npgsqlParameters);
                if (idResponse == 0)
                    throw new Exception("Zero rows affected.");
            });
        }

        public async Task ExecuteAsync(string query, object obj)
        {
            HandleConnection();
            await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var npgsqlParameters = CreateNpgsqlParameters(obj);
                var idResponse = await _dbContext.Database.ExecuteSqlRawAsync(query, npgsqlParameters);
                if (idResponse == 0)
                    throw new Exception("Zero rows affected.");
            });
        }

        public async Task<TReturn> ExecuteWithReturnAsync<TEntity, TReturn>(string query, TEntity entity)
        {
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var npgsqlParameters = CreateNpgsqlParameters(entity);

                // Para consultas escalares no EF Core, precisamos de uma abordagem modificada
                // No PostgreSQL, é melhor usar RETURNING
                query = $"SELECT ({query}) AS Result";
                var result = await _dbContext.Database.SqlQueryRaw<ScalarResult<TReturn>>(query, npgsqlParameters).FirstOrDefaultAsync();
                return result != null ? result.Result : default;
            });
        }

        public async Task<int> ExecuteAsync<T>(string query, object parameters)
        {
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var npgsqlParameters = CreateNpgsqlParameters(parameters);
                return await _dbContext.Database.ExecuteSqlRawAsync(query, npgsqlParameters);
            });
        }

        public async Task<int> ExecuteInBatch<T>(string query, object parameters)
        {
            HandleConnection();
            return await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                query = FormatSqlForPostgres(query);
                var npgsqlParameters = CreateNpgsqlParameters(parameters);
                return await _dbContext.Database.ExecuteSqlRawAsync(query, npgsqlParameters);
            });
        }

        public async Task<int> BulkInsert<T>(List<T> rows, string destinationTableName, bool keepIdentity = false)
        {
            HandleConnection();

            // Implementação de bulk insert para PostgreSQL usando Npgsql
            var npgsqlConn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
            var dataTable = ListtoDataTableConverter.ToDataTable(rows);
            int rowsAffected = 0;

            // Garantir que a conexão está aberta
            if (npgsqlConn.State != ConnectionState.Open)
                npgsqlConn.Open();

            // Obter nomes das colunas da tabela
            var columnNames = new List<string>();
            foreach (DataColumn column in dataTable.Columns)
            {
                // Ignorar a coluna ID se keepIdentity for false
                if (!keepIdentity && column.ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

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
                        // Pular a coluna ID se keepIdentity for false
                        if (!keepIdentity && dataTable.Columns[i].ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                            continue;

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
                query = FormatSqlForPostgres(query);
                foreach (var objeto in objetos)
                {
                    var npgsqlParameters = CreateNpgsqlParameters(objeto);
                    await _dbContext.Database.ExecuteSqlRawAsync(query, npgsqlParameters);
                }
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
                    query = FormatSqlForPostgres(query);
                    // Modificar a consulta para retornar o ID no PostgreSQL
                    var modifiedQuery = $"{query} RETURNING id";
                    var npgsqlParameters = CreateNpgsqlParameters(objeto);

                    var id = await _dbContext.Database.SqlQueryRaw<int>(modifiedQuery, npgsqlParameters).FirstOrDefaultAsync();
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
            var efTransaction = _dbContext.Database.BeginTransaction(isolationLevel);
            _transaction = efTransaction.GetDbTransaction();
            return _transaction;
        }

        public void Commit()
        {
            if (_transaction == null)
                throw new Exception("Nao é possível realizar commit sem abrir uma transaction");
            _dbContext.Database.CommitTransaction();
            _transaction = null;
        }

        public void Rollback()
        {
            if (_transaction == null)
                throw new Exception("Nao é possível realizar rollback sem abrir uma transaction");
            _dbContext.Database.RollbackTransaction();
            _transaction = null;
        }

        public async Task<PaginatedResult<T>> QueryPaginated<T>(string query, string orderByClause, object parameters, int pageNumber, int pageSize, bool readUncommitted = true)
        {
            var offset = (pageNumber - 1) * pageSize;

            // Modificar a sintaxe para PostgreSQL
            var paginatedQuery = $"{query} {orderByClause} LIMIT @PageSize OFFSET @Offset";
            var countQuery = $"SELECT COUNT(*) FROM ({query}) AS CountQuery";

            // Criar parâmetros incluindo paginação
            var paramDict = ConvertObjectToParameterDictionary(parameters);
            paramDict.Add("Offset", offset);
            paramDict.Add("PageSize", pageSize);

            if (readUncommitted)
            {
                countQuery = HandleReadUncommitted(countQuery, readUncommitted);
                paginatedQuery = HandleReadUncommitted(paginatedQuery, readUncommitted);
            }

            HandleConnection();

            countQuery = FormatSqlForPostgres(countQuery);
            paginatedQuery = FormatSqlForPostgres(paginatedQuery);

            var countParameters = CreateNpgsqlParameters(parameters);
            var totalCount = await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                var result = await _dbContext.Database.SqlQueryRaw<int>(countQuery, countParameters).ToListAsync();
                return result.FirstOrDefault();
            });

            var paginatedParameters = CreateNpgsqlParameters(paramDict);
            var items = await _asyncRetryPolicy.ExecuteAsync(async () =>
            {
                return await _dbContext.Database.SqlQueryRaw<T>(paginatedQuery, paginatedParameters).ToListAsync();
            });

            return new PaginatedResult<T>(items, totalCount, pageNumber, pageSize);
        }

        public IDbConnection Connection
        {
            get
            {
                _connection = _dbContext.Database.GetDbConnection();
                return _connection;
            }
        }

        public IDbTransaction? Transaction => _transaction;

        public void Dispose()
        {
            _dbContext?.Dispose();
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

            retryPolicyDBOpen.Execute(() =>
            {
                var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    _dbContext.Database.OpenConnection();
            });
        }

        private string HandleReadUncommitted(string query, bool readUncommitted)
        {
            // PostgreSQL não suporta READ UNCOMMITTED exatamente como SQL Server
            // O nível mais próximo é READ COMMITTED 
            if (readUncommitted)
                query = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED; " + query;

            return query;
        }

        private NpgsqlParameter[] CreateNpgsqlParameters(object parameters)
        {
            if (parameters == null)
                return Array.Empty<NpgsqlParameter>();

            var paramDict = ConvertObjectToParameterDictionary(parameters);

            return paramDict.Select(p => new NpgsqlParameter(p.Key, p.Value ?? DBNull.Value)).ToArray();
        }

        private Dictionary<string, object> ConvertObjectToParameterDictionary(object parameters)
        {
            var paramDict = new Dictionary<string, object>();

            if (parameters is IDictionary<string, object> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    paramDict.Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                var props = parameters.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var value = prop.GetValue(parameters);
                    paramDict.Add(prop.Name, value);
                }
            }

            return paramDict;
        }

        private string FormatSqlForPostgres(string query)
        {
            // Substituir sintaxe SQL Server por PostgreSQL
            // 1. Substituir @ em parâmetros por :
            // query = Regex.Replace(query, @"@(\w+)", ":$1");

            // 2. Substituir colchetes por aspas duplas
            query = query.Replace("[", "\"").Replace("]", "\"");

            // 3. Substituir TOP por LIMIT (casos simples)
            // Mais complexo, deixarei para implementação mais detalhada

            return query;
        }
    }
}