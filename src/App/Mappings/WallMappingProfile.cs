using Shared.Dtos;
using AutoMapper;
using Domain.Entities;

namespace App.Mappings;

public class WallMappingProfile : Profile
{
    public WallMappingProfile()
    {
        CreateMap<Wall, WallResponse>();
    }
}