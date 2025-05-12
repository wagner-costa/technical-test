using FluentValidation;
using FluentValidation.Results;

namespace CNPJ.Processing.Core.Extensions
{
    public static class FluentValidationExtensions
    {
        public static bool HasErrors(this ValidationResult validationResult)
        {
            var result = HasValidationFailuresBySeverity(validationResult, Severity.Error);

            return result;
        }

        public static bool HasWarnings(this ValidationResult validationResult)
        {
            var result = HasValidationFailuresBySeverity(validationResult, Severity.Warning);

            return result;
        }

        static bool HasValidationFailuresBySeverity(ValidationResult validationResult, Severity severity)
        {
            if (validationResult == null || validationResult.Errors == null)
                return false;

            var hasAny = validationResult.Errors.Any(x => x != null && x.Severity == severity);

            return hasAny;
        }
    }
}