using Api.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Api.Mappings;

public class WindowMappingProfile : Profile
{
    public WindowMappingProfile()
    {
        CreateMap<Window, WindowResponse>();
    }
}
