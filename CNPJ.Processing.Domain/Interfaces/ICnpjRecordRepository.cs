using CNPJ.Processing.Domain.Entities;
using CNPJ.Processing.Domain.Enum;

namespace CNPJ.Processing.Domain.Interfaces
{
    public interface ICnpjRecordRepository
    {
        Task<CnpjRecord> AddAsync(CnpjRecord record);
        Task UpdateAsync(CnpjRecord record, Guid id);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<CnpjRecord>> GetByStatusAsync(StatusEnum status);
        Task<CnpjRecord> GetByCnpjAsync(string cnpj);

    }
}
