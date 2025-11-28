using AutoMapper;
using LotusDharma.Application.Geography.Dtos;
using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.Geography.Mapping;

public class GeographyMappingProfile : Profile
{
    public GeographyMappingProfile()
    {
        CreateMap<Province, ProvinceDto>();
        CreateMap<Province, ProvinceGeoDto>()
            .IncludeBase<Province, ProvinceDto>()
            .ForMember(dest => dest.Geometry, opt => opt.Ignore());

        CreateMap<Commune, CommuneDto>();
        CreateMap<Commune, CommuneGeoDto>()
            .IncludeBase<Commune, CommuneDto>()
            .ForMember(dest => dest.Geometry, opt => opt.Ignore());
    }
}


