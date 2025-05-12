using CNPJ.Processing.Core;
using CNPJ.Processing.Domain.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CNPJ.Processing.Domain.Entities
{
    public class CnpjValidationInvalid
    {
        private CnpjValidationInvalid() { } 
        public string Cnpj { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;    
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow.TimeZoneBrasil();
        public string InvalidReason { get; set; } = string.Empty;
        public string InvalidationDetails { get; set; } = string.Empty;
        [JsonConverter(typeof(StringEnumConverter))]
        public DataSourceEnum DataSource { get; set; } = DataSourceEnum.Unknown;
        public string? ValidationStage { get; set; }

        internal CnpjValidationInvalid(string cnpj, string name, string requestId, DateTime validationTimestamp, string? invalidReason, string? invalidationDetails, DataSourceEnum dataSource, string? validationStage)
        {
            Cnpj = cnpj;
            Name = name;
            RequestId = requestId;
            ValidationTimestamp = validationTimestamp;
            InvalidReason = invalidReason ?? "CNPJ_CHECK_DIGIT_INVALID";
            InvalidationDetails = invalidationDetails ?? "Dígito verificador não corresponde ao algoritmo de validação";
            DataSource = dataSource;
            ValidationStage = validationStage ?? "FORMAT_VALIDATION";
        }

        public static CnpjValidationInvalid Create(string cnpj, string name, DataSourceEnum dataSource)
        {
            return new CnpjValidationInvalid(cnpj, name, Guid.NewGuid().ToString(), DateTime.UtcNow.TimeZoneBrasil(), null, null,  dataSource, null);
        }
    }
}

