using AutoMapper;

namespace CNPJ.Processing.Application.AutoMapper
{
    public class AutoMapperConfig
    {
        public static MapperConfiguration RegisterMappings()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<DomainToDTOMappingProfile>();
                cfg.AddProfile<DTOMappingProfileToDomain>();
                
            });
        }
    }
}
