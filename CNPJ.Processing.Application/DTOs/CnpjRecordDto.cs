using CNPJ.Processing.Core.Validators;

namespace CNPJ.Processing.Application.DTOs
{
    public class CnpjRecordDto
    {
        public string Cnpj { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public static CnpjRecordDto FromCnpjRecord(string cnpj, string name)
        {
            return new CnpjRecordDto
            {
                Cnpj = cnpj,
                Name = name
            };
        }

    }
}
