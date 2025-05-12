using System.ComponentModel;

namespace CNPJ.Processing.Domain.Enum
{
    public enum DataSourceEnum
    {
        [Description("Desconhecido")]
        Unknown = 0,
        [Description("Validação Local")]
        LocalValidation = 1,
        [Description("Base de Dados Interna")]
        InternalDatabase = 2,
        [Description("API de Parceiro")]
        PartnerApi = 3,
        [Description("Todas as Fontes")]
        All = 5
    }
}
