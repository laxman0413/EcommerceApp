using EcommerceApp.Application.Cart.DTOs;
using EcommerceApp.Application.Payments.DTOs;

namespace EcommerceApp.Application.Cart.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto);
    Task<CartDto> UpdateItemAsync(Guid userId, Guid productId, UpdateCartItemDto dto);
    Task<CartDto> RemoveItemAsync(Guid userId, Guid productId);
    Task<PaymentResultDto> CheckoutAsync(Guid userId, CheckoutRequestDto dto);
}
