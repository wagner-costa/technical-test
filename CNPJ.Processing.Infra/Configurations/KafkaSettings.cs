namespace CNPJ.Processing.Infra.Configurations
{
    /// <summary>
    /// Configurações do Kafka a serem lidas do appsettings.json
    /// </summary>
    public class KafkaSettings
    {
        public const string SectionName = "Kafka";
        public string? BootstrapServers { get; set; } 
        public int SocketTimeoutMs { get; set; } = 45000;
        public int MessageTimeoutMs { get; set; } = 1000000;
        public int DefaultRetryCount { get; set; } = 3;
        public int DefaultRetryDelayMs { get; set; } = 3000;
        public string? GroupCNPJValidade { get; set; }
        public string? TopicCnpjProcessingRequests { get; set; }
        public string? TopicCnpjValidationValid { get; set; }
        public string? TopicCnpjValidationInvalid { get; set; }
    }
}



