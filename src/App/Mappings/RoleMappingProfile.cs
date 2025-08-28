using Shared.Dtos;
using AutoMapper;
using Domain.Entities;

namespace App.Mappings;

public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Role, RoleDto>();
    }
}