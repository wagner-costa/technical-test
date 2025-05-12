namespace CNPJ.Processing.Infra.Kafka
{
    public static class KafkaHelper
    {
        private static IKafkaHelper _instance;

        public static void Configure(IKafkaHelper kafkaHelper) =>
            _instance = kafkaHelper ?? throw new ArgumentNullException(nameof(kafkaHelper));

        public static Task<string> SendMessage<T>(string topic, T data, bool resendOnError = true)
        {
            EnsureConfigured();
            return _instance.SendMessage(topic, data, resendOnError);
        }

        public static Task ConsumeAsync<T>(
            CancellationToken cancellationToken,
            string topic,
            string groupId,
            Func<T, CancellationToken, Task> messageHandler,
            bool resendOnError = true)
        {
            EnsureConfigured();
            return _instance.ConsumeAsync(cancellationToken, topic, groupId, messageHandler, resendOnError);
        }

        private static void EnsureConfigured()
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("KafkaHelper não foi configurado. Chame KafkaHelper.Configure() durante a inicialização da aplicação.");
            }
        }
    }
}

