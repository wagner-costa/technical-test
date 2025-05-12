using AutoMapper;
using CNPJ.Processing.Application.DTOs;
using CNPJ.Processing.Application.Services;
using CNPJ.Processing.Domain.Entities;
using CNPJ.Processing.Domain.Enum;
using CNPJ.Processing.Domain.Interfaces;
using CNPJ.Processing.Infra.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CNPJ.Processing.Tests
{
    public class CNPJRecordServiceTests
    {
        private readonly Mock<ILogger<CNPJRecordService>> _loggerMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<ICnpjRecordRepository> _repositoryMock = new();
        private readonly KafkaSettings _kafkaSettings = new()
        {
            TopicCnpjValidationValid = "valid-topic",
            TopicCnpjValidationInvalid = "invalid-topic"
        };

        private readonly CNPJRecordService _service;

        public CNPJRecordServiceTests()
        {
            var optionsMock = Options.Create(_kafkaSettings);
            _service = new CNPJRecordService(_loggerMock.Object, _mapperMock.Object, _repositoryMock.Object, optionsMock);
        }

        [Fact]
        public async Task AddAsync_CnpjJaExiste_DeveRetornarErro()
        {
            // Arrange
            var dto = new CnpjRecordDto { Cnpj = "12.345.678/0001-95", Name = "Empresa XYZ" };
            var formattedCnpj = "12.345.678/0001-95";

            var existingRecord = CnpjRecord.CreateValidate(formattedCnpj, "Empresa XYZ", DataSourceEnum.LocalValidation, StatusEnum.Valid, true);

            _repositoryMock.Setup(x => x.GetByCnpjAsync(It.IsAny<string>()))
                           .ReturnsAsync(existingRecord);

            // Act
            var result = await _service.AddAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.ValidationResult.Errors, e => e.ErrorMessage.Contains("já existe"));
        }

        [Fact]
        public async Task AddAsync_ShouldReturnInvalidResponse_WhenCnpjFormatIsInvalid()
        {
            // Arrange
            var invalidCnpj = "123"; // CNPJ inválido (menos de 14 dígitos)

            var mockLogger = new Mock<ILogger<CNPJRecordService>>();
            var mockMapper = new Mock<IMapper>();
            var mockRepo = new Mock<ICnpjRecordRepository>();
            var mockOptions = Options.Create(new KafkaSettings());

            var service = new CNPJRecordService(mockLogger.Object, mockMapper.Object, mockRepo.Object, mockOptions);

            var dto = new CnpjRecordDto { Cnpj = invalidCnpj, Name = "Empresa Teste" };

            // Act
            var result = _service.ValidateCnpjFormat(invalidCnpj);    

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Result.isValid);    
        }

        [Fact]
        public async Task AddAsync_ShouldReturnValidResponse_WhenCnpjFormatIsValid()
        {
            // Arrange
            var validCnpj = "25.533.645/0001-10"; // CNPJ inválido (menos de 14 dígitos)

            var mockLogger = new Mock<ILogger<CNPJRecordService>>();
            var mockMapper = new Mock<IMapper>();
            var mockRepo = new Mock<ICnpjRecordRepository>();
            var mockOptions = Options.Create(new KafkaSettings());

            // Act
            var result = _service.ValidateCnpjFormat(validCnpj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Result.isValid);
        }
    }
}