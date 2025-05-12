using CNPJ.Processing.Core;
using CNPJ.Processing.Domain.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CNPJ.Processing.Domain.Entities
{
    public class CnpjValidationValid
    {
        private CnpjValidationValid() { }   
        public string Cnpj { get; set; } = string.Empty;    
        public string RequestId { get; set; } = string.Empty;   
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow.TimeZoneBrasil();
        public string Name { get; set; } = string.Empty;
        public DateTime OpenValid { get; set; } = DateTime.UtcNow.TimeZoneBrasil();
        [JsonConverter(typeof(StringEnumConverter))]
        public StatusEnum Status { get; set; } = StatusEnum.Valid;
        [JsonConverter(typeof(StringEnumConverter))]
        public DataSourceEnum DataSource { get; set; } = DataSourceEnum.Unknown;

        public static CnpjValidationValid Create(string cnpj, string name, DataSourceEnum dataSource)
        {
            return new CnpjValidationValid
            {
                Cnpj = cnpj,
                RequestId = Guid.NewGuid().ToString(),
                ValidationTimestamp = DateTime.UtcNow.TimeZoneBrasil(),
                Name = name,
                OpenValid = DateTime.UtcNow.TimeZoneBrasil(),
                Status = StatusEnum.Valid,
                DataSource = dataSource
            };
        }

    }
}
