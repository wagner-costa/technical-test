namespace CNPJ.Processing.Infra.Configurations
{
    public class DatabaseSettings
    {
        public const string SectionName = "Database";

        /// <summary>
        /// UseEntityFramework para Definir Entity Framework ou Dapper
        /// </summary>
        public bool UseEntityFramework { get; set; } = false;

        /// <summary>
        /// CommandTimeout para conexao
        /// </summary>
        public int CommandTimeout { get; set; } = 30;
        /// <summary>
        /// EnableDetailedLogging para Logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
    }
}
