using AutoMapper;
using EcommerceApp.Application.Products.DTOs;
using EcommerceApp.Application.Products.Services;
using EcommerceApp.Application.Tests.TestSupport;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using Moq;

namespace EcommerceApp.Application.Tests.Products;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repo = new();
    private readonly IMapper _mapper = MapperFactory.Create();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repo.Object, _mapper);
    }

    [Fact]
    public async Task GetAllAsync_PassesFiltersAndPagingThroughAndReturnsMappedPage()
    {
        var products = new List<Product>
        {
            new() { Name = "Widget", Category = "Tools", Price = 9.99m, StockQuantity = 5 }
        };
        _repo.Setup(r => r.GetAllAsync("Tools", "Wid", true, 2, 10)).ReturnsAsync((products, 21));

        var result = await _sut.GetAllAsync("Tools", "Wid", true, 2, 10);

        Assert.Single(result.Items);
        Assert.Equal("Widget", result.Items[0].Name);
        Assert.True(result.Items[0].IsAvailable);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(21, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public async Task GetAllAsync_NonPositivePage_ClampsToFirstPage(int requestedPage, int expectedPage)
    {
        _repo.Setup(r => r.GetAllAsync(null, null, null, expectedPage, 20)).ReturnsAsync((new List<Product>(), 0));

        var result = await _sut.GetAllAsync(null, null, null, requestedPage, 20);

        Assert.Equal(expectedPage, result.Page);
        _repo.Verify(r => r.GetAllAsync(null, null, null, expectedPage, 20), Times.Once);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 100)]
    public async Task GetAllAsync_PageSizeOutOfRange_ClampsToAllowedBounds(int requestedPageSize, int expectedPageSize)
    {
        _repo.Setup(r => r.GetAllAsync(null, null, null, 1, expectedPageSize)).ReturnsAsync((new List<Product>(), 0));

        var result = await _sut.GetAllAsync(null, null, null, 1, requestedPageSize);

        Assert.Equal(expectedPageSize, result.PageSize);
        _repo.Verify(r => r.GetAllAsync(null, null, null, 1, expectedPageSize), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsMappedDto()
    {
        var product = new Product { Name = "Gadget", Category = "Tech", Price = 19.99m, StockQuantity = 0 };
        _repo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result!.Id);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task CreateAsync_AddsProductAndSavesChanges()
    {
        var dto = new CreateProductDto { Name = "New", Category = "Cat", Price = 5m, StockQuantity = 10 };

        var result = await _sut.CreateAsync(dto);

        _repo.Verify(r => r.AddAsync(It.Is<Product>(p => p.Name == "New" && p.Category == "Cat" && p.Price == 5m)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        Assert.Equal("New", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ProductNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateProductDto { Name = "X" });

        Assert.Null(result);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OnlySetsProvidedFields_LeavesOthersUnchanged()
    {
        var product = new Product
        {
            Name = "Original",
            Category = "OriginalCat",
            Price = 10m,
            StockQuantity = 3,
            Description = "Original description"
        };
        _repo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        // Only Price is provided; every other field is null and must be left untouched by the
        // UpdateProductDto -> Product mapping's null-skip condition.
        var dto = new UpdateProductDto { Price = 25m };

        var result = await _sut.UpdateAsync(product.Id, dto);

        Assert.NotNull(result);
        Assert.Equal(25m, result!.Price);
        Assert.Equal("Original", result.Name);
        Assert.Equal("OriginalCat", result.Category);
        Assert.Equal("Original description", result.Description);
        _repo.Verify(r => r.UpdateAsync(product), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ProductNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ProductFound_DeletesAndReturnsTrue()
    {
        var product = new Product { Name = "ToDelete" };
        _repo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.DeleteAsync(product.Id);

        Assert.True(result);
        _repo.Verify(r => r.DeleteAsync(product.Id), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
