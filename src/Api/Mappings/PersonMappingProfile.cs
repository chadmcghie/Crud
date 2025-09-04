using Api.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Api.Mappings;

public class PersonMappingProfile : Profile
{
    public PersonMappingProfile()
    {
        CreateMap<Person, PersonResponse>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles));
        
        CreateMap<Role, RoleResponse>();
    }
}