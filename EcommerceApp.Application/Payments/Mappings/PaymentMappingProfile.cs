using AutoMapper;
using EcommerceApp.Application.Payments.DTOs;
using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Application.Payments.Mappings;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        // Status (PaymentStatus enum) -> Status (string) via AutoMapper's enum-to-string
        // convention. Every other field matches by name, so no ForMember calls are needed.
        CreateMap<Payment, PaymentResultDto>();
    }
}
