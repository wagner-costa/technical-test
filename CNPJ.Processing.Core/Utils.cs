namespace CNPJ.Processing.Core
{
    public static class Utils
    {
        public static bool IsValidCpfCnpj(string field)
        {
            return (IsCpf(field) || IsCnpj(field));
        }

        private static bool IsCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            cpf = cpf.Trim().Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;

            for (int j = 0; j < 10; j++)
                if (j.ToString().PadLeft(11, char.Parse(j.ToString())) == cpf)
                    return false;

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cpf.EndsWith(digito);
        }

        private static bool IsCnpj(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }

        public static DateTime GetDate()
        {
            var brasil = TimeZoneConverter.TZConvert.GetTimeZoneInfo("E. South America Standard Time");
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, brasil);
        }

        public static bool IsAny<T>(this IEnumerable<T> data)
        {
            return data != null && data.Any();
        }

        public static string ObterHoraBrasilia()
        {
            DateTime timeUtc = DateTime.UtcNow;
            string hora;

            try
            {
                var brasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                hora = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, brasilia).ToString("HH:mm:ss");
            }
            catch (Exception)
            {
                hora = timeUtc.ToString("HH:mm:ss");
            }

            return hora;
        }

        public static DateTime TimeZoneBrasil(this DateTime date)
        {
            var tzBrasil = "E. South America Standard Time";
            var brasil = TimeZoneConverter.TZConvert.GetTimeZoneInfo(tzBrasil);
#if !DEBUG
            // Conversão para o formato de timezone IANA utilizado no linux
            var tzBrasilIana = TimeZoneConverter.TZConvert.WindowsToIana(tzBrasil);
            brasil = TimeZoneConverter.TZConvert.GetTimeZoneInfo(tzBrasilIana);
#endif
            return TimeZoneInfo.ConvertTime(date, brasil);
        }
    }
}

