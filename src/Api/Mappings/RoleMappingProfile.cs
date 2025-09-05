using Api.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Api.Mappings;

public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Role, RoleDto>();
    }
}
