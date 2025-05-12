using CNPJ.Processing.Domain.Entities;
using CNPJ.Processing.Domain.Enum;
using CNPJ.Processing.Domain.Interfaces;
using CNPJ.Processing.Infra.DB;

namespace CNPJ.Processing.Infra.Data.Repositories
{
    public class CnpjRecordRepository : ICnpjRecordRepository
    {
        private readonly IPostgreSqlContext _postgreSqlContext;

        public CnpjRecordRepository(IPostgreSqlContext postgreSqlContext) => 
            _postgreSqlContext = postgreSqlContext;
        public async Task<CnpjRecord> AddAsync(CnpjRecord record)
        {
            return await _postgreSqlContext.QueryFirstOrDefault<CnpjRecord>(@"
                INSERT INTO 
                    cnpj_record 
                    (id, cnpj, name, validated_at, status, is_valid_format, dataSource)
                VALUES 
                    (@Id
                    , @Cnpj
                    , @Name
                    , @ValidatedAt
                    , @Status
                    , @IsValidFormat
                    , @DataSource)
            ", 
            new
            {
                record.Id,
                record.Cnpj,
                record.Name,
                record.ValidatedAt,
                Status = record.Status.ToString(),
                record.IsValidFormat,
                record.DataSource,
            }, false);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _postgreSqlContext.ExecuteAsync<CnpjRecord>(@"
                DELETE FROM 
                  cnpj_record
                WHERE 
                  id = @id
                ", new
                {
                    id
                });
        }

        public async Task<CnpjRecord> GetByCnpjAsync(string cnpj) =>
            await _postgreSqlContext.QueryFirstOrDefault<CnpjRecord>(@"SELECT * FROM cnpj_record WHERE cnpj=@cnpj", new { cnpj }, false);

        public async Task<IEnumerable<CnpjRecord>> GetByStatusAsync(StatusEnum status)
        {
            return await _postgreSqlContext.Query<CnpjRecord>(@"SELECT * FROM cnpj_record");
        }

        public async Task UpdateAsync(CnpjRecord record, Guid id)
        {
            await _postgreSqlContext.ExecuteAsync<CnpjRecord>(@"
                UPDATE 
                  cnpj_record
                SET 
                  validated_at = @ValidatedAt
                  , name = @Name
                  , is_valid_format = @IsValidFormat
                  , status = @Status
                WHERE 
                  id = @id
                ", new
            {
                record.ValidatedAt,
                record.Name,
                record.IsValidFormat,
                Status = StatusEnum.Blocked.ToString(),
                id
            });
        }
    }
}
