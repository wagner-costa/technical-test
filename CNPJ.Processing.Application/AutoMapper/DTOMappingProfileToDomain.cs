using AutoMapper;
using CNPJ.Processing.Application.DTOs;
using CNPJ.Processing.Domain.Entities;

namespace CNPJ.Processing.Application.AutoMapper
{
    public class DTOMappingProfileToDomain : Profile
    {
        public DTOMappingProfileToDomain()
        {
            CreateMap<CnpjRecordResponse, CnpjRecord>();
            CreateMap<CnpjRecordUpdDto, CnpjRecord>();
        }
    }
}
