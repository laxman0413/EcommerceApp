using AutoMapper;
using EcommerceApp.Application.Cart.DTOs;
using EcommerceApp.Application.Cart.Services;
using EcommerceApp.Application.Common.Exceptions;
using EcommerceApp.Application.Payments.DTOs;
using EcommerceApp.Application.Payments.Services;
using EcommerceApp.Application.Tests.TestSupport;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceApp.Application.Tests.Cart;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _cartRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IPaymentService> _paymentService = new();
    private readonly IMapper _mapper = MapperFactory.Create();
    private readonly CartService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CartServiceTests()
    {
        _sut = new CartService(
            _cartRepository.Object,
            _productRepository.Object,
            _paymentService.Object,
            _mapper,
            new Mock<ILogger<CartService>>().Object);
    }

    [Fact]
    public async Task GetCartAsync_ReturnsMappedItemsWithComputedTotal()
    {
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "Widget", UnitPrice = 10m, Quantity = 2, StockQuantity = 5 }
        });

        var result = await _sut.GetCartAsync(_userId);

        Assert.Single(result.Items);
        Assert.Equal(20m, result.TotalAmount);
    }

    [Fact]
    public async Task AddItemAsync_ProductDoesNotExist_ThrowsNotFound()
    {
        var productId = Guid.NewGuid();
        _productRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _sut.AddItemAsync(_userId, new AddCartItemDto { ProductId = productId, Quantity = 1 }));
    }

    [Fact]
    public async Task AddItemAsync_NewItem_ExceedsStock_ThrowsConflict()
    {
        var product = new Product { Name = "Widget", StockQuantity = 3 };
        _productRepository.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _cartRepository.Setup(r => r.GetItemAsync(_userId, product.Id)).ReturnsAsync((CartItem?)null);

        await Assert.ThrowsAsync<ConflictAppException>(
            () => _sut.AddItemAsync(_userId, new AddCartItemDto { ProductId = product.Id, Quantity = 4 }));

        _cartRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_AlreadyInCart_CombinedQuantityExceedsStock_ThrowsConflict()
    {
        var product = new Product { Name = "Widget", StockQuantity = 5 };
        var existing = new CartItem { UserId = _userId, ProductId = product.Id, Quantity = 3 };
        _productRepository.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _cartRepository.Setup(r => r.GetItemAsync(_userId, product.Id)).ReturnsAsync(existing);

        // 3 already in cart + 3 more requested = 6, which exceeds the 5 in stock.
        await Assert.ThrowsAsync<ConflictAppException>(
            () => _sut.AddItemAsync(_userId, new AddCartItemDto { ProductId = product.Id, Quantity = 3 }));

        _cartRepository.Verify(r => r.UpdateQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_NewItem_WithinStock_AddsToCart()
    {
        var product = new Product { Name = "Widget", StockQuantity = 5 };
        _productRepository.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _cartRepository.Setup(r => r.GetItemAsync(_userId, product.Id)).ReturnsAsync((CartItem?)null);
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>());

        await _sut.AddItemAsync(_userId, new AddCartItemDto { ProductId = product.Id, Quantity = 2 });

        _cartRepository.Verify(r => r.AddAsync(It.Is<CartItem>(i =>
            i.UserId == _userId && i.ProductId == product.Id && i.Quantity == 2)), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_AlreadyInCart_WithinStock_UpdatesQuantity()
    {
        var product = new Product { Name = "Widget", StockQuantity = 10 };
        var existing = new CartItem { UserId = _userId, ProductId = product.Id, Quantity = 2 };
        _productRepository.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _cartRepository.Setup(r => r.GetItemAsync(_userId, product.Id)).ReturnsAsync(existing);
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>());

        await _sut.AddItemAsync(_userId, new AddCartItemDto { ProductId = product.Id, Quantity = 3 });

        _cartRepository.Verify(r => r.UpdateQuantityAsync(existing.Id, 5), Times.Once);
        _cartRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemAsync_ItemNotInCart_ThrowsNotFound()
    {
        _cartRepository.Setup(r => r.GetItemAsync(_userId, It.IsAny<Guid>())).ReturnsAsync((CartItem?)null);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _sut.UpdateItemAsync(_userId, Guid.NewGuid(), new UpdateCartItemDto { Quantity = 1 }));
    }

    [Fact]
    public async Task UpdateItemAsync_ExceedsStock_ThrowsConflict()
    {
        var product = new Product { Name = "Widget", StockQuantity = 2 };
        var item = new CartItem { UserId = _userId, ProductId = product.Id, Quantity = 1 };
        _cartRepository.Setup(r => r.GetItemAsync(_userId, product.Id)).ReturnsAsync(item);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        await Assert.ThrowsAsync<ConflictAppException>(
            () => _sut.UpdateItemAsync(_userId, product.Id, new UpdateCartItemDto { Quantity = 5 }));

        _cartRepository.Verify(r => r.UpdateQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemAsync_Valid_UpdatesQuantity()
    {
        var product = new Product { Name = "Widget", StockQuantity = 10 };
        var item = new CartItem { UserId = _userId, ProductId = product.Id, Quantity = 1 };
        _cartRepository.Setup(r => r.GetItemAsync(_userId, product.Id)).ReturnsAsync(item);
        _productRepository.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>());

        await _sut.UpdateItemAsync(_userId, product.Id, new UpdateCartItemDto { Quantity = 4 });

        _cartRepository.Verify(r => r.UpdateQuantityAsync(item.Id, 4), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_ItemNotInCart_ThrowsNotFound()
    {
        _cartRepository.Setup(r => r.GetItemAsync(_userId, It.IsAny<Guid>())).ReturnsAsync((CartItem?)null);

        await Assert.ThrowsAsync<NotFoundAppException>(() => _sut.RemoveItemAsync(_userId, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveItemAsync_Valid_RemovesItem()
    {
        var item = new CartItem { UserId = _userId, ProductId = Guid.NewGuid(), Quantity = 1 };
        _cartRepository.Setup(r => r.GetItemAsync(_userId, item.ProductId)).ReturnsAsync(item);
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>());

        await _sut.RemoveItemAsync(_userId, item.ProductId);

        _cartRepository.Verify(r => r.RemoveAsync(item.Id), Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_EmptyCart_ThrowsConflict()
    {
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>());

        await Assert.ThrowsAsync<ConflictAppException>(
            () => _sut.CheckoutAsync(_userId, new CheckoutRequestDto()));

        _paymentService.Verify(p => p.ChargeAsync(It.IsAny<ChargeRequestDto>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_StockChangedSinceAddToCart_ThrowsConflictAndDoesNotCharge()
    {
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "Widget", UnitPrice = 10m, Quantity = 5, StockQuantity = 2 }
        });

        await Assert.ThrowsAsync<ConflictAppException>(
            () => _sut.CheckoutAsync(_userId, new CheckoutRequestDto()));

        _paymentService.Verify(p => p.ChargeAsync(It.IsAny<ChargeRequestDto>(), It.IsAny<Guid>()), Times.Never);
        _cartRepository.Verify(r => r.ClearAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_PaymentDeclined_PropagatesAndLeavesCartIntact()
    {
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "Widget", UnitPrice = 10m, Quantity = 1, StockQuantity = 5 }
        });
        _paymentService
            .Setup(p => p.ChargeAsync(It.IsAny<ChargeRequestDto>(), _userId))
            .ThrowsAsync(new PaymentDeclinedAppException("card_declined"));

        await Assert.ThrowsAsync<PaymentDeclinedAppException>(
            () => _sut.CheckoutAsync(_userId, new CheckoutRequestDto()));

        _cartRepository.Verify(r => r.ClearAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_Success_ChargesComputedTotalAndClearsCart()
    {
        _cartRepository.Setup(r => r.GetDetailedByUserIdAsync(_userId)).ReturnsAsync(new List<CartItemDetail>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "Widget", UnitPrice = 10m, Quantity = 2, StockQuantity = 5 },
            new() { ProductId = Guid.NewGuid(), ProductName = "Gadget", UnitPrice = 5m, Quantity = 3, StockQuantity = 5 }
        });

        var expectedResult = new PaymentResultDto { Id = Guid.NewGuid(), Status = "Succeeded", Amount = 35m };
        _paymentService
            .Setup(p => p.ChargeAsync(It.Is<ChargeRequestDto>(c => c.Amount == 35m), _userId))
            .ReturnsAsync(expectedResult);

        var dto = new CheckoutRequestDto { Currency = "usd", CardNumber = "4242 4242 4242 4242" };
        var result = await _sut.CheckoutAsync(_userId, dto);

        Assert.Same(expectedResult, result);
        _cartRepository.Verify(r => r.ClearAsync(_userId), Times.Once);
        _paymentService.Verify(p => p.ChargeAsync(
            It.Is<ChargeRequestDto>(c => c.CardNumber == "4242 4242 4242 4242" && c.Currency == "usd" && c.Amount == 35m),
            _userId), Times.Once);
    }
}
