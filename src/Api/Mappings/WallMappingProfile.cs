using Api.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Api.Mappings;

public class WallMappingProfile : Profile
{
    public WallMappingProfile()
    {
        CreateMap<Wall, WallResponse>();
    }
}