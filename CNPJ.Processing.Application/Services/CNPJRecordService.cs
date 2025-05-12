using AutoMapper;
using CNPJ.Processing.Application.DTOs;
using CNPJ.Processing.Application.Interfaces;
using CNPJ.Processing.Core.Extensions;
using CNPJ.Processing.Core.Validators;
using CNPJ.Processing.Domain.Entities;
using CNPJ.Processing.Domain.Enum;
using CNPJ.Processing.Domain.Interfaces;
using CNPJ.Processing.Infra.Configurations;
using CNPJ.Processing.Infra.Kafka;
using CNPJ.Processing.Infra.Kafka.Models;
using CsvHelper.Configuration;
using CsvHelper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CNPJ.Processing.Application.Services
{
    public class CNPJRecordService : ICNPJRecordService
    {
        private static readonly Regex NonDigitRegex = new Regex(@"\D", RegexOptions.Compiled);

        private readonly IMapper _mapper;
        private readonly ILogger<CNPJRecordService> _logger;
        private readonly ICnpjRecordRepository _cnpjRecordRepository;
        private readonly KafkaSettings _kafkaSettings;

        public CNPJRecordService(ILogger<CNPJRecordService> logger, IMapper mapper, ICnpjRecordRepository cnpjRecordRepository, IOptions<KafkaSettings> options) =>
            (_logger, _mapper, _cnpjRecordRepository, _kafkaSettings) = (logger, mapper, cnpjRecordRepository, options.Value);

        public async Task<CnpjRecordResponse> AddAsync(CnpjRecordDto cnpjRecord)
        {
            _logger.LogInformation("Adicionando CNPJ: {Cnpj}", cnpjRecord.Cnpj);
            var (isValid, formattedCnpj, response) = await ValidateCnpjFormat(cnpjRecord.Cnpj);

            var existingRecord = await _cnpjRecordRepository.GetByCnpjAsync(formattedCnpj);

            if (existingRecord != null)
            {
                response.Name = existingRecord.Name;    
                response.AddErrorValidation(ErrorCodes.BAD_REQUEST, "O CNPJ já existe em nossa base de dados.");
                _logger.LogWarning("O CNPJ já existe em nossa base de dados. {Number}", cnpjRecord.Cnpj);
                return response;
            }

            var record = CnpjRecord.CreateValidate(formattedCnpj, cnpjRecord.Name, DataSourceEnum.LocalValidation, isValid ? StatusEnum.Valid : StatusEnum.Invalid, isValid);

            await _cnpjRecordRepository.AddAsync(record);

            if (isValid)
                await SendMessageCNPJToKafka(CnpjValidationValid.Create(formattedCnpj, cnpjRecord.Name, DataSourceEnum.LocalValidation) , _kafkaSettings.TopicCnpjValidationValid ?? throw new InvalidOperationException($"Falha ao enviar mensagem para o tópico {_kafkaSettings.TopicCnpjValidationValid}"));
            else
                await SendMessageCNPJToKafka(CnpjValidationInvalid.Create(formattedCnpj, cnpjRecord.Name, DataSourceEnum.LocalValidation), _kafkaSettings.TopicCnpjValidationInvalid ?? throw new InvalidOperationException($"Falha ao enviar mensagem para o tópico {_kafkaSettings.TopicCnpjValidationInvalid}"));

            return _mapper.Map<CnpjRecordResponse>(record);
        }

        public async Task<CnpjRecordResponse> DeleteAsync(string number)
        {
            _logger.LogInformation("Deletando CNPJ: {Cnpj}", number);   
            var (isValid, formattedCnpj, response) = await ValidateCnpjFormat(number);

            if (!isValid)
                return response;

            var existingRecord = await _cnpjRecordRepository.GetByCnpjAsync(formattedCnpj);

            if (existingRecord == null)
            {
                response.AddErrorValidation(ErrorCodes.BAD_REQUEST, "O CNPJ não existe em nossa base de dados.");
                _logger.LogError("O CNPJ não existe em nossa base de dados. {Number}", number);
                return response;
            }

            await _cnpjRecordRepository.DeleteAsync(existingRecord.Id);

            existingRecord.Update(StatusEnum.Delete);

            return _mapper.Map<CnpjRecordResponse>(existingRecord);
        }

        public async Task<CnpjRecordResponse> GetByNumberAsync(string number)
        {
            var (isValid, formattedCnpj, response) = await ValidateCnpjFormat(number);

            if (!isValid)
                return response;

            var record = await _cnpjRecordRepository.GetByCnpjAsync(formattedCnpj);

            return record != null
                ? _mapper.Map<CnpjRecordResponse>(record)
                : response;
        }

        public async Task<IEnumerable<CnpjRecordResponse>> GetByStatusAsync(StatusEnum status) => 
            _mapper.Map<IEnumerable<CnpjRecordResponse>>(await _cnpjRecordRepository.GetByStatusAsync(status));

        public async Task<CnpjRecordResponse> UpdateAsync(CnpjRecordUpdDto cnpjRecord)
        {
            var (isValid, formattedCnpj, response) = await ValidateCnpjFormat(cnpjRecord.Cnpj);

            if (!isValid)
            {
                // await SendMessageCNPJToKafka(new CnpjRecordDto { Cnpj = formattedCnpj, Name = cnpjRecord.Name }, isValid);
                return response;
            }

            var existingRecord = await _cnpjRecordRepository.GetByCnpjAsync(formattedCnpj);

            if (existingRecord == null)
            {
                response.AddErrorValidation(ErrorCodes.BAD_REQUEST, "O CNPJ não existe em nossa base de dados.");
                return response;
            }

            existingRecord.Update(cnpjRecord.Status);

            await _cnpjRecordRepository.UpdateAsync(existingRecord, existingRecord.Id);

            return _mapper.Map<CnpjRecordResponse>(existingRecord);
        }

        public async Task<UploadCSVResponse> UploadFileAsync(IFormFile file)
        {
            UploadCSVResponse response = new();

            try
            {
                if (!ValidarArquivoCsv(file))
                {
                    response.AddErrorValidation(ErrorCodes.BAD_REQUEST, "Arquivo inválido ou não é um CSV");
                    return response;
                }

                var records = await ProcessarfileAsync(file);

                response.TotalRecords = records.Count();
                response.Records = records;
                return response;
            }
            catch (Exception ex)
            {
                response.AddErrorValidation(ErrorCodes.BAD_REQUEST, $"Erro ao processar o arquivo: {ex.Message}");
                return response;
            }
        }

        public async Task<(bool isValid, string formattedCnpj, CnpjRecordResponse response)> ValidateCnpjFormat(string number)
        {
            var cleanCnpj = NonDigitRegex.Replace(number, "");
            bool hasValidFormat = cleanCnpj.Length == 14;
            var formattedCnpj = hasValidFormat ? CnpjValidatorExtension.FormatCnpj(cleanCnpj) : cleanCnpj;

            var validFormat = (!hasValidFormat) ?
                InvalidFormatCnpj(formattedCnpj, cleanCnpj) :
                await ValidateCnpj(formattedCnpj);

            return validFormat;
        }

        private (bool isValid, string formattedCnpj, CnpjRecordResponse response) InvalidFormatCnpj(string formattedCnpj, string cleanCnpj)
        {
            CnpjRecordResponse response = new() { Cnpj = formattedCnpj, Status = StatusEnum.Invalid, IsValidFormat = false };
            response.AddErrorValidation(ErrorCodes.BAD_REQUEST, "O CNPJ não possui o formato válido.");
            _logger.LogError("O CNPJ não é válido. {Number}", cleanCnpj);
            return (false, cleanCnpj, response);
        }

        private async Task<(bool isValid, string formattedCnpj, CnpjRecordResponse response)> ValidateCnpj(string formattedCnpj)
        {
            var cnpjValidator = new InlineValidator<string>();
            cnpjValidator.RuleFor(x => x).IsValidCnpj();
            var validationResult = await cnpjValidator.ValidateAsync(formattedCnpj);

            CnpjRecordResponse response = new() { Cnpj = formattedCnpj, Status = validationResult.IsValid ? StatusEnum.Valid : StatusEnum.Invalid, IsValidFormat = validationResult.IsValid };

            if (!validationResult.IsValid)
            {
                _logger.LogError("O CNPJ não é válido. {Number}", formattedCnpj);
                response.AddErrorValidation(ErrorCodes.BAD_REQUEST, "O CNPJ não é válido.");
                return (false, formattedCnpj, response);
            }

            return (true, formattedCnpj, response);
        }

        private async Task SendMessageCNPJToKafka<TEntity>(TEntity cnpjRecord, string topic)
        {
            await KafkaHelper.SendMessage(topic, new DataTopic<TEntity>
            {
                Message = cnpjRecord,
                Topic = topic
            });
        }

        private async Task<IEnumerable<CnpjProcessingRequest>> ProcessarfileAsync(IFormFile file)
        {
            if (!ValidarArquivoCsv(file))
                throw new ArgumentException("Arquivo inválido ou não é um CSV");

            try
            {
                var records = new List<CnpjProcessingRequest>();

                using var stream = new StreamReader(file.OpenReadStream());
                while (!stream.EndOfStream)
                {
                    var line = await stream.ReadLineAsync();
                    var values = line.Split(',');

                    var cnpjProcessingRequest = CnpjProcessingRequest.Create(values[0], values[1]);

                    await SendMessageCNPJToKafka(cnpjProcessingRequest, _kafkaSettings.TopicCnpjProcessingRequests ?? throw new InvalidOperationException($"Falha ao enviar mensagem para o tópico {_kafkaSettings.TopicCnpjProcessingRequests}"));

                    records.Add(cnpjProcessingRequest);
                }

                return records; 
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao processar o arquivo: {Message}", ex.Message);
                throw new Exception($"Erro ao processar o arquivo: {ex.Message}");  
            }
        }

        public bool ValidarArquivoCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".txt" && !string.IsNullOrEmpty(extension))
                return false;

            if (!string.IsNullOrEmpty(file.ContentType) &&
                file.ContentType != "text/csv" &&
                file.ContentType != "application/csv" &&
                file.ContentType != "text/plain" &&
                file.ContentType != "application/vnd.ms-excel")
            {
                return false;
            }

            return true;
        }
    }
}