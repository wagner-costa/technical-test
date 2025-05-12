using CNPJ.Processing.Application.DTOs;
using CsvHelper.Configuration;

namespace CNPJ.Processing.Application.AutoMapper
{
    public sealed class CnpjRecordMap : ClassMap<CnpjRecordDto>
    {
        public CnpjRecordMap()
        {
            Map(m => m.Cnpj).Index(0);
            Map(m => m.Name).Index(1);
        }
    }
}
