using CNPJ.Processing.Domain.Enum;
using CNPJ.Processing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CNPJ.Processing.Domain.Entities
{
    public class CnpjRecord
    {
        private CnpjRecord() { }

        public CnpjRecord(string cnpj, string name, StatusEnum status, DataSourceEnum dataSource, bool isValidFormat)
        {
            Id = Guid.NewGuid();
            Name = name;
            Cnpj = !string.IsNullOrWhiteSpace(cnpj) ? cnpj : throw new ArgumentException("CNPJ não pode ser nulo ou vazio", nameof(cnpj));
            ValidatedAt = DateTime.UtcNow.TimeZoneBrasil();
            Status = status;
            IsValidFormat = isValidFormat;
            DataSource = dataSource;    
        }

        internal CnpjRecord(Guid id, string cnpj, string name, DateTime validatedAt, StatusEnum status, DataSourceEnum dataSource, bool isValidFormat)
        {
            Id = id;
            Cnpj = cnpj;
            ValidatedAt = validatedAt;
            Status = status;
            IsValidFormat = isValidFormat;
            DataSource = dataSource;    
        }
        public Guid Id { get; private set; }
        public string Cnpj { get; private set; }
        public string Name { get; private set; }
        public DateTime ValidatedAt { get; private set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public StatusEnum Status { get; private set; }
        public bool IsValidFormat { get; private set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DataSourceEnum DataSource { get; set; } = DataSourceEnum.Unknown;
        public void Update(StatusEnum newStatus)
        {
            Status = newStatus;
            ValidatedAt = DateTime.UtcNow.TimeZoneBrasil();
        }

        public static CnpjRecord CreateValidate(string cnpj, string name, DataSourceEnum dataSource, StatusEnum status, bool isValidFormat) =>
            new CnpjRecord(cnpj, name, status, dataSource, isValidFormat);
    }
}