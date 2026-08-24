using AutoMapper;
using EcommerceApp.Application.Auth.DTOs;
using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Application.Auth.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}
