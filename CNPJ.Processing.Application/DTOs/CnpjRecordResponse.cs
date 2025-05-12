using CNPJ.Processing.Domain.Enum;
using CNPJ.Processing.Core.Validators;
using Newtonsoft.Json;
using CNPJ.Processing.Infra.Converters;
using Newtonsoft.Json.Converters;

namespace CNPJ.Processing.Application.DTOs
{
    public class CnpjRecordResponse : Validator
    {
        public Guid Id { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;    
        public DateTime ValidatedAt { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public StatusEnum Status { get; set; }
        public bool IsValidFormat { get; set; }
        public override bool IsValid()
        {
            return ValidationResult.IsValid;
        }
    }
}
