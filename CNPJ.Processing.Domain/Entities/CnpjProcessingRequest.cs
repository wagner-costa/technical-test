using CNPJ.Processing.Core;
using CNPJ.Processing.Domain.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CNPJ.Processing.Domain.Entities
{
    public class CnpjProcessingRequest
    {
        private CnpjProcessingRequest() { }
        public string Cnpj { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string RequestId { get; private set; } = Guid.NewGuid().ToString();
        public string Source { get; private set; } = "Api";
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow.TimeZoneBrasil();
        [JsonConverter(typeof(StringEnumConverter))]
        public PriorityEnum Priority { get; private set; } = PriorityEnum.Normal;

        public CnpjProcessingRequest(string cnpj, string name, string source, DateTime timestamp, PriorityEnum priority)
        {
            Cnpj = cnpj;
            Name = name;
            RequestId = Guid.NewGuid().ToString();
            Source = source;
            Timestamp = timestamp;
            Priority = priority;
        }

        internal CnpjProcessingRequest(string cnpj, string name, string requestId, string source, DateTime timestamp, PriorityEnum priority)
        {
            Cnpj = cnpj;
            Name = name;
            RequestId = requestId;
            Source = source;
            Timestamp = timestamp;
            Priority = priority;
        }

        public static CnpjProcessingRequest Create(string cnpj, string name, PriorityEnum priority = PriorityEnum.Normal)
        {
            return new CnpjProcessingRequest(cnpj, name, Guid.NewGuid().ToString(), "Api", DateTime.UtcNow.TimeZoneBrasil(), priority);
        }
    }
}