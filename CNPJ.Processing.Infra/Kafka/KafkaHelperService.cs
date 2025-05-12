using CNPJ.Processing.Infra.Configurations;
using CNPJ.Processing.Infra.Kafka.Exceptions;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;

namespace CNPJ.Processing.Infra.Kafka
{
    public class KafkaHelperService : IKafkaHelper
    {
        private readonly ILogger<KafkaHelperService> _logger;
        private readonly KafkaSettings _settings;

        public KafkaHelperService(ILogger<KafkaHelperService> logger, IOptions<KafkaSettings> options) =>
            (_logger, _settings) = (logger, options.Value ?? throw new ArgumentNullException(nameof(options)));

        public async Task<string> SendMessage<T>(string topic, T data, bool resendOnError = true)
        {
            ValidateKafkaConfiguration();

            try
            {
                return await SendMessageInternal(topic, data);
            }
            catch (KafkaHelperException ex)
            {
                if (resendOnError)
                {
                    await Task.Delay(_settings.DefaultRetryDelayMs);
                    return await SendMessage(topic, data, false);
                }

                _logger.LogError(ex, "Falha ao enviar mensagem para o tópico {Topic}", topic);  
                return JsonConvert.SerializeObject(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar mensagem para o tópico {Topic}", topic);  
                await Task.Delay(_settings.DefaultRetryDelayMs);
                return JsonConvert.SerializeObject($"Task.Delay: {ex.Message}");
            }
        }

        public async Task ConsumeAsync<T>(
            CancellationToken cancellationToken,
            string topic,
            string groupId,
            Func<T, CancellationToken, Task> messageHandler,
            bool resendOnError = true)
        {
            ValidateKafkaConfiguration();

            var consumer = await CreateConsumer(groupId, topic);

            try
            {
                await RunConsumerLoop(consumer, messageHandler, cancellationToken, topic, resendOnError);
            }
            finally
            {
                try { consumer.Close(); } catch { }
                try { consumer.Dispose(); } catch { }
            }
        }

        private async Task RunConsumerLoop<T>(
            IConsumer<string, string> consumer,
            Func<T, CancellationToken, Task> messageHandler,
            CancellationToken cancellationToken,
            string topic,
            bool resendOnError)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                object data = default;

                try
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    if (consumeResult == null) continue;

                    var metadata = JsonConvert.DeserializeObject<T>(consumeResult.Message.Value);
                    data = metadata;

                    await messageHandler(metadata, cancellationToken);
                    consumer.Commit(consumeResult);
                }
                catch (KafkaHelperException)
                {
                    if (resendOnError && data != default)
                        await ResendFailedMessage(topic, data, true);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    _logger.LogError($"Falha ao processar mensagem do tópico {topic}", data);   
                    await Task.Delay(_settings.DefaultRetryDelayMs, cancellationToken);
                }
            }
        }

        private async Task<string> SendMessageInternal<T>(string topic, T data)
        {
            var config = CreateProducerConfig();

            var retryPolicy = Policy
                .Handle<KafkaHelperException>()
                .WaitAndRetryAsync(
                    retryCount: _settings.DefaultRetryCount,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(3));

            return await retryPolicy.ExecuteAsync(() => SendWithProducer(data, topic, config));
        }

        private Task<string> SendWithProducer<T>(T data, string topic, ProducerConfig config)
        {
            using var producer = new ProducerBuilder<string, string>(config).Build();
            try
            {
                var messageKey = Guid.NewGuid().ToString();
                var messageValue = JsonConvert.SerializeObject(data);

                var sendResult = producer
                    .ProduceAsync(topic, new Message<string, string> { Key = messageKey, Value = messageValue })
                    .GetAwaiter()
                    .GetResult();

                if (sendResult.Status == PersistenceStatus.NotPersisted)
                {
                    _logger.LogError($"Falha ao persistir mensagem no Kafka: {sendResult.Status.ToString()}", data);
                    throw new Exception("Falha ao persistir mensagem no Kafka");
                }

                return Task.FromResult($"Mensagem '{sendResult.Value}', Status '{sendResult.Status}' de '{sendResult.TopicPartitionOffset}'");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao enviar mensagem para o tópico {topic}: {ex.Message}", data);
                throw new KafkaHelperException($"Falha na entrega da mensagem para tópico {topic}", ex);
            }
        }

        private ProducerConfig CreateProducerConfig()
        {
            var baseConfig = CreateBaseConfig();

            return new ProducerConfig(baseConfig)
            {
                MessageTimeoutMs = _settings.MessageTimeoutMs
            };
        }

        private ConsumerConfig CreateConsumerConfig(string groupId)
        {
            var baseConfig = CreateBaseConfig();

            return new ConsumerConfig(baseConfig)
            {
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AutoCommitIntervalMs = 1000,
                EnableAutoCommit = false
            };
        }

        private ClientConfig CreateBaseConfig()
        {
            return new ClientConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                SocketTimeoutMs = _settings.SocketTimeoutMs
            };
        }

        private async Task<IConsumer<string, string>> CreateConsumer(string groupId, string topic)
        {
            var consumerConfig = CreateConsumerConfig(groupId);
            var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(topic);

            return await Task.FromResult(consumer);
        }

        private void ValidateKafkaConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_settings.BootstrapServers))
                throw new KafkaHelperInvalidArgumentsException("BootstrapServers", null);
        }

        private async Task ResendFailedMessage(string topic, object data, bool isConsume)
        {
            if (data != default)
            {
                await Task.Delay(_settings.DefaultRetryDelayMs);
                var prefixTopic = isConsume ? "Consume" : "Send";
                var failTopic = $"{topic}_{prefixTopic}_Fail";
                await SendMessage(failTopic, data, false);
                _logger.LogError($"Falha ao enviar mensagem para o tópico {failTopic}", data);
            }
        }
    }
}