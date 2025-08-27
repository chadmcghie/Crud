using Shared.Dtos;
using AutoMapper;
using Domain.Entities;

namespace App.Mappings;

public class WindowMappingProfile : Profile
{
    public WindowMappingProfile()
    {
        CreateMap<Window, WindowResponse>();
    }
}