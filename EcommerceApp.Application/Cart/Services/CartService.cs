using AutoMapper;
using EcommerceApp.Application.Cart.DTOs;
using EcommerceApp.Application.Common.Exceptions;
using EcommerceApp.Application.Payments.DTOs;
using EcommerceApp.Application.Payments.Services;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcommerceApp.Application.Cart.Services;

public class CartService(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    IPaymentService paymentService,
    IMapper mapper,
    ILogger<CartService> logger) : ICartService
{
    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var details = await cartRepository.GetDetailedByUserIdAsync(userId);
        return new CartDto { Items = mapper.Map<List<CartItemDto>>(details) };
    }

    public async Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto)
    {
        var product = await productRepository.GetByIdAsync(dto.ProductId)
            ?? throw new NotFoundAppException($"Product {dto.ProductId} not found");

        var existing = await cartRepository.GetItemAsync(userId, dto.ProductId);
        var newQuantity = (existing?.Quantity ?? 0) + dto.Quantity;

        if (newQuantity > product.StockQuantity)
            throw new ConflictAppException($"Only {product.StockQuantity} of '{product.Name}' left in stock");

        if (existing is null)
        {
            var item = mapper.Map<CartItem>(dto);
            item.UserId = userId;
            await cartRepository.AddAsync(item);
        }
        else
        {
            await cartRepository.UpdateQuantityAsync(existing.Id, newQuantity);
        }

        logger.LogInformation("User {UserId} added {Quantity} x {ProductId} to cart", userId, dto.Quantity, dto.ProductId);
        return await GetCartAsync(userId);
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid productId, UpdateCartItemDto dto)
    {
        var item = await cartRepository.GetItemAsync(userId, productId)
            ?? throw new NotFoundAppException("That product is not in your cart");

        var product = await productRepository.GetByIdAsync(productId)
            ?? throw new NotFoundAppException($"Product {productId} not found");

        if (dto.Quantity > product.StockQuantity)
            throw new ConflictAppException($"Only {product.StockQuantity} of '{product.Name}' left in stock");

        await cartRepository.UpdateQuantityAsync(item.Id, dto.Quantity);

        logger.LogInformation("User {UserId} set {ProductId} quantity to {Quantity}", userId, productId, dto.Quantity);
        return await GetCartAsync(userId);
    }

    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid productId)
    {
        var item = await cartRepository.GetItemAsync(userId, productId)
            ?? throw new NotFoundAppException("That product is not in your cart");

        await cartRepository.RemoveAsync(item.Id);

        logger.LogInformation("User {UserId} removed {ProductId} from cart", userId, productId);
        return await GetCartAsync(userId);
    }

    public async Task<PaymentResultDto> CheckoutAsync(Guid userId, CheckoutRequestDto dto)
    {
        var details = await cartRepository.GetDetailedByUserIdAsync(userId);
        if (details.Count == 0)
            throw new ConflictAppException("Your cart is empty");

        // Re-validate against the live catalog at the moment of payment, not just at
        // add-to-cart time — stock may have moved since items were added.
        foreach (var line in details)
        {
            if (line.Quantity > line.StockQuantity)
                throw new ConflictAppException($"Only {line.StockQuantity} of '{line.ProductName}' left in stock");
        }

        var total = details.Sum(d => d.UnitPrice * d.Quantity);

        var chargeRequest = mapper.Map<ChargeRequestDto>(dto);
        chargeRequest.Amount = total;

        // PaymentService throws PaymentDeclinedAppException/PaymentGatewayAppException on
        // failure — that exception propagates straight out of this method and the cart is
        // never cleared, so the user can simply retry checkout with a different card.
        var result = await paymentService.ChargeAsync(chargeRequest, userId);

        await cartRepository.ClearAsync(userId);
        logger.LogInformation("User {UserId} checked out cart for {Amount} {Currency}", userId, total, dto.Currency);

        return result;
    }
}
