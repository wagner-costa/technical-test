using CNPJ.Processing.Domain.Enum;

namespace CNPJ.Processing.Application.DTOs
{
    public class CnpjRecordUpdDto
    {
        public string Cnpj { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;    
        public StatusEnum Status { get; set; } = StatusEnum.Pending;
    }
}
