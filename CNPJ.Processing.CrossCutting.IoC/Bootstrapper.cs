using CNPJ.Processing.Application.AutoMapper;
using CNPJ.Processing.Application.Handlers;
using CNPJ.Processing.Application.Interfaces;
using CNPJ.Processing.Application.Services;
using CNPJ.Processing.Domain.Interfaces;
using CNPJ.Processing.Infra.Configurations;
using CNPJ.Processing.Infra.Data.Repositories;
using CNPJ.Processing.Infra.DB;
using CNPJ.Processing.Infra.Enums;
using CNPJ.Processing.Infra.Factory;
using CNPJ.Processing.Infra.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CNPJ.Processing.CrossCutting.IoC
{
    public static class Bootstrapper
    {
        public static IServiceCollection AddSetupIoC(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
            services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
            services.AddSingleton<IKafkaHelper, KafkaHelperService>();

            services.AddSingleton<IPostgreSqlContext>(sp => {
                var config = sp.GetRequiredService<IConfiguration>();
                var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
                var connectionString = config.GetConnectionString("DefaultConnection");

                return PostgreSqlContextFactory.Create(
                    connectionString ?? throw new Exception("Erro de captura da conexao do banco de dados"),
                    dbSettings.UseEntityFramework ? OrmTypeEnum.EntityFramework : OrmTypeEnum.Dapper
                );
            });
            
            services.ConfigureAutoMapper();
            services.AddRepositories();
            services.RegisterServices();
            return services;
        }

        private static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<ICNPJRecordService, CNPJRecordService>();

            services.AddHostedService<HostServiceHandler>();
        }

        private static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<ICnpjRecordRepository, CnpjRecordRepository>();
        }
        private static void ConfigureAutoMapper(this IServiceCollection services)
        {
            var mapperConfig = AutoMapperConfig.RegisterMappings();
            services.AddAutoMapper(typeof(DomainToDTOMappingProfile), typeof(DTOMappingProfileToDomain));
        }
    }
}

