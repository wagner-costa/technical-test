using CNPJ.Processing.Application.DTOs;
using CNPJ.Processing.Domain.Enum;
using Microsoft.AspNetCore.Http;

namespace CNPJ.Processing.Application.Interfaces
{
    public interface ICNPJRecordService
    {
        Task<CnpjRecordResponse> AddAsync(CnpjRecordDto cnpjRecord);
        Task<IEnumerable<CnpjRecordResponse>> GetByStatusAsync(StatusEnum status);
        Task<CnpjRecordResponse> GetByNumberAsync(string number);
        Task<CnpjRecordResponse> UpdateAsync(CnpjRecordUpdDto cnpjRecord);
        Task<CnpjRecordResponse> DeleteAsync(string number);
        Task<UploadCSVResponse> UploadFileAsync(IFormFile file);

        Task<(bool isValid, string formattedCnpj, CnpjRecordResponse response)> ValidateCnpjFormat(string number);
    }
}
