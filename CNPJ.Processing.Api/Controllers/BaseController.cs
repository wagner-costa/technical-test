using CNPJ.Processing.Core.Extensions;
using CNPJ.Processing.Infra.Models;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace CNPJ.Processing.Api.Controllers
{
    [ApiController]
    public abstract class BaseController<TController> : ControllerBase
    {
        protected IHttpContextAccessor _httpContextAccessor;
        protected readonly string _requestId;

        public BaseController()
        {
            _requestId = Guid.NewGuid().ToString();
        }

        private IActionResult MakeResponse(IActionResult result)
        {
            Response.Headers.Append("X-Request-Id", _requestId);
            return result;
        }

        protected IActionResult ResponseCreated<T>(string route, T dto)
        {
            IActionResult response = null;
            if (dto != null)
            {
                response = Created(route, response);
            }
            else
            {
                response = BadRequest("Item not found");
            }
            return MakeResponse(response);
        }

        protected IActionResult CreateResponse<T>(T dto)
        {
            IActionResult response = null;
            if (dto != null)
            {
                response = Ok(dto);
            }
            else
            {
                response = NotFound("Item not found");
            }
            return MakeResponse(response);
        }

        protected IActionResult CreateValidationResponse(ValidationResult validationResult)
        {
            var message = new StringBuilder();
            int status = HttpStatusCode.Unauthorized.GetHashCode();

            if (validationResult.HasErrors())
            {
                foreach (var error in validationResult.Errors)
                {
                    message.AppendLine(error.ErrorMessage);
                    status = Convert.ToInt32(error.ErrorCode);
                }

                status = HttpStatusCode.BadRequest.GetHashCode();
            }

            if (validationResult.HasWarnings())
            {
                foreach (var error in validationResult.Errors)
                {
                    message.AppendLine(error.ErrorMessage);
                    status = Convert.ToInt32(error.ErrorCode);
                }
            }

            return MakeResponse(StatusCode(
                status,
                new ErrorResponse()
                {
                    Status = status,
                    Message = $"{_requestId} :: {message}"
                }
            ));
        }     
    }
}
