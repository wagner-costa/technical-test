using CNPJ.Processing.Infra.DB;
using CNPJ.Processing.Infra.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNPJ.Processing.Infra.Factory
{
    public class PostgreSqlContextFactory
    {
        public static IPostgreSqlContext Create(string connectionString, OrmTypeEnum ormType)
        {
            return ormType switch
            {
                OrmTypeEnum.Dapper => new PostgreSqlDapperContext(connectionString),
                OrmTypeEnum.EntityFramework => new PostgreSqlEFContext(connectionString),
                _ => throw new ArgumentException("ORM type not supported", nameof(ormType))
            };
        }
    }
}
