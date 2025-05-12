using CNPJ.Processing.Core.Validators;
using CNPJ.Processing.Domain.Entities;

namespace CNPJ.Processing.Application.DTOs
{
    public class UploadCSVResponse : Validator
    {
        public int TotalRecords { get; set; } = 0;
        public IEnumerable<CnpjProcessingRequest> Records { get; set; } = new List<CnpjProcessingRequest>();
        public override bool IsValid()
        {
            return ValidationResult.IsValid;
        }
    }
}
