using AutoMapper;
using CNPJ.Processing.Application.DTOs;
using CNPJ.Processing.Domain.Entities;

namespace CNPJ.Processing.Application.AutoMapper
{
    public class DomainToDTOMappingProfile : Profile
    {
        public DomainToDTOMappingProfile()
        {
            CreateMap<CnpjRecord, CnpjRecordResponse>();
            CreateMap<CnpjRecord, CnpjRecordDto>();
        }
    }
}
