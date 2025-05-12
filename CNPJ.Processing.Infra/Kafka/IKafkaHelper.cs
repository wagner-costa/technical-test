namespace CNPJ.Processing.Infra.Kafka
{
    /// <summary>
    /// Interface para KafkaHelper, facilitando testes unitários e reduzindo acoplamento
    /// </summary>
    public interface IKafkaHelper
    {
        Task<string> SendMessage<T>(string topic, T data, bool resendOnError = true);
        Task ConsumeAsync<T>(CancellationToken cancellationToken, string topic, string groupId, Func<T, CancellationToken, Task> messageHandler, bool resendOnError = true);
    }
}
