using EcommerceApp.Application.Products.DTOs;

namespace EcommerceApp.Application.Products.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(string? category, string? search, bool? inStockOnly);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto);
    Task<bool> DeleteAsync(Guid id);
}