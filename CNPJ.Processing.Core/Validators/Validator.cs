using FluentValidation;
using FluentValidation.Results;

namespace CNPJ.Processing.Core.Validators
{
    public abstract class Validator
    {
        public ValidationResult ValidationResult { get; protected set; }

        protected Validator()
        {
            ValidationResult = new ValidationResult();
        }

        public void AddErrorValidation(int errorStatus, string description)
        {
            var failure = new ValidationFailure(errorStatus.ToString(), description);
            failure.Severity = Severity.Error;
            failure.ErrorCode = errorStatus.ToString();
            ValidationResult.Errors.Add(failure);
        }

        public void AddWarningValidation(int warningStatus, string description)
        {
            var failure = new ValidationFailure(warningStatus.ToString(), description);
            failure.Severity = Severity.Warning;
            failure.ErrorCode = warningStatus.ToString();
            ValidationResult.Errors.Add(failure);
        }

        public void AddWarningValidationFromFluentValidationList(IEnumerable<ValidationFailure> validationFailures)
        {
            if (validationFailures == null || !validationFailures.Any())
                return;

            foreach (var falha in validationFailures)
            {
                ValidationResult.Errors.Add(falha);
            }
        }

        public void AddValidationResults(ValidationResult validation)
        {
            ValidationResult = validation;
        }

        public abstract bool IsValid();
    }
}
