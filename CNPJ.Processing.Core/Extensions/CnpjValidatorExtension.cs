using FluentValidation;
using System.Text.RegularExpressions;

namespace CNPJ.Processing.Core.Extensions
{
    public static class CnpjValidatorExtension
    {
        /// <summary>
        /// Extensão para FluentValidation que valida se um CNPJ é válido
        /// </summary>
        public static IRuleBuilderOptions<T, string> IsValidCnpj<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(BeValidCnpj)
                .WithMessage("O CNPJ '{PropertyValue}' não é válido.");
        }
        public static IRuleBuilderOptions<T, string> HasValidCnpjFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(HasValidFormat)
                .WithMessage("O CNPJ deve estar no formato XX.XXX.XXX/XXXX-XX ou conter apenas 14 dígitos numéricos.");
        }
        private static bool HasValidFormat(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            bool isFormatted = Regex.IsMatch(cnpj, @"^\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}$");
            bool isUnformatted = Regex.IsMatch(cnpj, @"^\d{14}$");

            return isFormatted || isUnformatted;
        }
        private static bool BeValidCnpj(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            string cnpjNumbers = Regex.Replace(cnpj, "[^0-9]", "");

            if (cnpjNumbers.Length != 14)
                return false;

            if (cnpjNumbers.Distinct().Count() == 1)
                return false;

            int[] multiplier1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplier2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            int sum = 0;
            for (int i = 0; i < 12; i++)
                sum += int.Parse(cnpjNumbers[i].ToString()) * multiplier1[i];

            int remainder = sum % 11;
            int digit1 = remainder < 2 ? 0 : 11 - remainder;

            if (int.Parse(cnpjNumbers[12].ToString()) != digit1)
                return false;

            sum = 0;
            for (int i = 0; i < 13; i++)
                sum += int.Parse(cnpjNumbers[i].ToString()) * multiplier2[i];

            remainder = sum % 11;
            int digit2 = remainder < 2 ? 0 : 11 - remainder;

            return int.Parse(cnpjNumbers[13].ToString()) == digit2;
        }

        public static string FormatCnpj(string cnpj)
        {
            string cnpjNumbers = Regex.Replace(cnpj, "[^0-9]", "");

            if (cnpjNumbers.Length != 14)
                return cnpj;

            return Convert.ToUInt64(cnpjNumbers).ToString(@"00\.000\.000\/0000\-00");
        }
    }
}
