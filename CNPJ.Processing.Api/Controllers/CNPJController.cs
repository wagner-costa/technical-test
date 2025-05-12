using Asp.Versioning;
using CNPJ.Processing.Application.DTOs;
using CNPJ.Processing.Application.Interfaces;
using CNPJ.Processing.Infra.Models;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using CNPJ.Processing.Domain.Entities;

namespace CNPJ.Processing.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("cnpjProcessing/api/v{version:apiVersion}/[controller]")]
    [Controller]
    public class CNPJController : BaseController<CNPJController>
    {
        private readonly ILogger<CNPJController> _logger;
        private readonly ICNPJRecordService _cnpjRecordService;

        public CNPJController(ILogger<CNPJController> logger, ICNPJRecordService cnpjRecordService) => 
            (_logger, _cnpjRecordService) = (logger, cnpjRecordService);

        /// <summary>
        /// Consulta CNPJ pelo número
        /// </summary>
        /// <param name="number">Número do CNPJ a ser consultado</param>
        /// <returns>Dados do CNPJ consultado</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CnpjRecordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByCnpj([FromQuery] string number)
        {
            _logger.LogInformation("Consultando CNPJ: {Number}", number);

            var response = await _cnpjRecordService.GetByNumberAsync(number);

            return !response.ValidationResult.IsValid
                ? CreateValidationResponse(response.ValidationResult)
                : CreateResponse(response);
        }

        /// <summary>
        /// Adiciona um novo CNPJ
        /// </summary>
        /// <param name="cnpjRecord">Dados do CNPJ a ser adicionado</param>
        /// <returns>Resultado da operação</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CnpjRecordResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCnpj([FromBody] CnpjRecordDto cnpjRecord)
        {
            _logger.LogInformation("Adicionando CNPJ: {Cnpj}", cnpjRecord.Cnpj);

            var response = await _cnpjRecordService.AddAsync(cnpjRecord);

            return !response.ValidationResult.IsValid
                ? CreateValidationResponse(response.ValidationResult)
                : ResponseCreated(nameof(CreateCnpj), response);
        }

        /// <summary>
        /// Atualiza um CNPJ existente
        /// </summary>
        /// <param name="cnpjRecord">Dados atualizados do CNPJ</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut]
        [ProducesResponseType(typeof(CnpjRecordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCnpj([FromBody] CnpjRecordUpdDto cnpjRecord)
        {
            _logger.LogInformation("Atualizando CNPJ: {Cnpj}", cnpjRecord.Cnpj);

            var response = await _cnpjRecordService.UpdateAsync(cnpjRecord);

            return !response.ValidationResult.IsValid
                ? CreateValidationResponse(response.ValidationResult)
                : CreateResponse(response);
        }

        /// <summary>
        /// Remove um CNPJ
        /// </summary>
        /// <param name="number">Número do CNPJ a ser removido</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCnpj([FromRoute] string number)
        {
            _logger.LogInformation("Removendo CNPJ: {Number}", number);

            var response = await _cnpjRecordService.DeleteAsync(number);

            return !response.ValidationResult.IsValid
                ? CreateValidationResponse(response.ValidationResult)
                : NoContent();
        }

        /// <summary>
        /// Consulta, Grava e Atualiza um CNPJ
        /// </summary>
        /// <param name="file">Dados em arquivo CSV dos para serem consultados CNPJ</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut("upload")]
        [ProducesResponseType(typeof(CnpjRecordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadCSV(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Arquivo não fornecido ou vazio");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Apenas arquivos CSV são permitidos");

            var response = await _cnpjRecordService.UploadFileAsync(file);  

            return !response.ValidationResult.IsValid
                ? CreateValidationResponse(response.ValidationResult)
                : CreateResponse(response);
        }
    }
}
