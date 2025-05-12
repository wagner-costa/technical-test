using AutoMapper;
using CNPJ.Processing.Application.DTOs;
using CNPJ.Processing.Application.Interfaces;
using CNPJ.Processing.Domain.Entities;
using CNPJ.Processing.Infra.Configurations;
using CNPJ.Processing.Infra.Kafka;
using CNPJ.Processing.Infra.Kafka.Exceptions;
using CNPJ.Processing.Infra.Kafka.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CNPJ.Processing.Application.Handlers
{
    public class HostServiceHandler : IHostedService
    {
        private readonly ILogger<HostServiceHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _services;
        private readonly IKafkaHelper _kafkaHelper;
        private readonly List<Task> _backgroundTasks = new List<Task>();
        private readonly KafkaSettings _kafkaSettings;

        public HostServiceHandler(ILogger<HostServiceHandler> logger, IMapper mapper, IServiceProvider services, IKafkaHelper kafkaHelper, IOptions<KafkaSettings> options) =>
            (_logger, _mapper, _services, _kafkaHelper, _kafkaSettings) = (logger, mapper, services, kafkaHelper, options.Value);

        public Task StartAsync(CancellationToken cancellationToken)
        {
#if (DEBUG)
            InitializeConsumerTasks(cancellationToken);
#endif
            return Task.CompletedTask;
        }
        private void InitializeConsumerTasks(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando o consumo de mensagens do Kafka...");
            var consumers = new List<(string topic, string group, Type messageType, Delegate handler)>
            {
                (_kafkaSettings.TopicCnpjProcessingRequests ?? throw new ArgumentNullException(nameof(_kafkaSettings.TopicCnpjProcessingRequests)),
                 _kafkaSettings.GroupCNPJValidade ?? throw new ArgumentNullException(nameof(_kafkaSettings.GroupCNPJValidade)),
                 typeof(DataTopic<CnpjProcessingRequest>),
                 new Func<DataTopic<CnpjProcessingRequest>, CancellationToken, Task>((message, ct) =>
                    ExecuteCnpjProcessingRequestInTheDatabase(_kafkaSettings.TopicCnpjProcessingRequests!, message, ct))),
                /*
                (_kafkaSettings.TopicCnpjValidationInvalid ?? throw new ArgumentNullException(nameof(_kafkaSettings.TopicCnpjValidationInvalid)),
                 _kafkaSettings.GroupCNPJValidade ?? throw new ArgumentNullException(nameof(_kafkaSettings.GroupCNPJValidade)),
                 typeof(DataTopic<CnpjValidationInvalid>),
                 new Func<DataTopic<CnpjValidationInvalid>, CancellationToken, Task>((message, ct) =>
                    ExecuteCnpjValidationInvalidInTheDatabase(_kafkaSettings.TopicCnpjValidationInvalid!, message, ct))),

                (_kafkaSettings.TopicCnpjValidationValid ?? throw new ArgumentNullException(nameof(_kafkaSettings.TopicCnpjValidationValid)),
                 _kafkaSettings.GroupCNPJValidade ?? throw new ArgumentNullException(nameof(_kafkaSettings.GroupCNPJValidade)),
                 typeof(DataTopic<CnpjValidationValid>),
                 new Func<DataTopic<CnpjValidationValid>, CancellationToken, Task>((message, ct) =>
                    ExecuteCnpjValidationValidInTheDatabase(_kafkaSettings.TopicCnpjValidationValid!, message, ct))),
                */

            };

            foreach (var (topic, group, messageType, handler) in consumers)
            {
                _logger.LogInformation($"Iniciando o consumo do tópico {topic} com o grupo {group} e o tipo de mensagem {messageType.Name}.");
                Task consumerTask = StartConsumerForTopicWithType(cancellationToken, topic, group, messageType, handler);
                _backgroundTasks.Add(consumerTask);
            }
        }

        private Task StartConsumerForTopicWithType(CancellationToken cancellationToken,  string topic, string group, Type messageType, Delegate handler)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var method = typeof(IKafkaHelper).GetMethod(nameof(IKafkaHelper.ConsumeAsync));
                    if (method == null)
                    {
                        throw new InvalidOperationException($"Method {nameof(IKafkaHelper.ConsumeAsync)} not found on {nameof(IKafkaHelper)}.");
                    }

                    var genericMethod = method.MakeGenericMethod(messageType);
                    if (genericMethod == null)
                    {
                        throw new InvalidOperationException($"Failed to create generic method for {nameof(IKafkaHelper.ConsumeAsync)}.");
                    }

                    await (Task)genericMethod.Invoke(_kafkaHelper, new object[]
                    {
                        cancellationToken,
                        topic,
                        group,
                        handler,
                        true
                    })!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao consumir mensagens do tópico {topic}: {ex.Message}");    
                    throw new KafkaHelperException($"Erro ao consumir mensagem da fila: {topic}", ex);
                }
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando o consumo de mensagens do Kafka...");
            foreach (var task in _backgroundTasks)
            {
                if (!task.IsCompleted)
                {
                    task.Wait(cancellationToken);
                }
            }
            _logger.LogInformation("Consumo de mensagens do Kafka parado.");
            return Task.CompletedTask;
        }           

        private async Task ExecuteCnpjProcessingRequestInTheDatabase(string topico, DataTopic<CnpjProcessingRequest> metadata, CancellationToken cancellationToken)
        {
            try
            {                              
                _logger.LogInformation($"Consumo fila {topico}", metadata); 
                using (var scope = _services.CreateScope())
                {
                    var cnpjRecordService = scope.ServiceProvider.GetRequiredService<ICNPJRecordService>();
                    await cnpjRecordService.AddAsync(CnpjRecordDto.FromCnpjRecord(metadata.Message.Cnpj, metadata.Message.Name));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao consumir mensagens do tópico {topico}: {ex.Message}");
                throw new KafkaHelperException($"Erro ao consumir mensagem da fila: {topico}", ex);
            }
        }

        private async Task ExecuteCnpjValidationInvalidInTheDatabase(string topico, DataTopic<CnpjValidationInvalid> metadata, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Consumo fila {topico}", metadata);

                using (var scope = _services.CreateScope())
                {
                    var cnpjRecordService = scope.ServiceProvider.GetRequiredService<ICNPJRecordService>();
                    // await cnpjRecordService.AddAsync(metadata.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao consumir mensagens do tópico {topico}: {ex.Message}");
                throw new KafkaHelperException($"Erro ao consumir mensagem da fila: {topico}", ex);
            }
        }

        private async Task ExecuteCnpjValidationValidInTheDatabase(string topico, DataTopic<CnpjValidationValid> metadata, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Consumo fila {topico}", metadata);

                using (var scope = _services.CreateScope())
                {
                    var cnpjRecordService = scope.ServiceProvider.GetRequiredService<ICNPJRecordService>();
                    // await cnpjRecordService.AddAsync(metadata.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao consumir mensagens do tópico {topico}: {ex.Message}");
                throw new KafkaHelperException($"Erro ao consumir mensagem da fila: {topico}", ex);
            }
        }



    }
}