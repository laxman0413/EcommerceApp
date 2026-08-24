using AutoMapper;
using EcommerceApp.Application.Cart.DTOs;
using EcommerceApp.Application.Payments.DTOs;
using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Application.Cart.Mappings;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        // Read side: the CartItems+Products join projection -> the response DTO.
        // CartItemDto.LineTotal is computed (UnitPrice * Quantity) and has no setter, so
        // AutoMapper just leaves it alone — same as Product.IsAvailable in ProductMappingProfile.
        CreateMap<CartItemDetail, CartItemDto>();

        // Write side: AddCartItemDto -> CartItem. UserId isn't part of the request body — it
        // comes from the authenticated caller's JWT — so it's excluded here and set by the
        // caller (CartService) right after mapping.
        CreateMap<AddCartItemDto, CartItem>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore());

        // CheckoutRequestDto and ChargeRequestDto share every card field by name. Amount has
        // no source member (it's computed from the cart total, never taken from the client)
        // and is set explicitly after mapping — see CartService.CheckoutAsync.
        CreateMap<CheckoutRequestDto, ChargeRequestDto>();
    }
}
