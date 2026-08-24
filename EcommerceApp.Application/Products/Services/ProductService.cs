using AutoMapper;
using EcommerceApp.Application.Products.DTOs;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;

namespace EcommerceApp.Application.Products.Services;

public class ProductService(IProductRepository repo, IMapper mapper) : IProductService
{
    public async Task<List<ProductDto>> GetAllAsync(string? category, string? search, bool? inStockOnly)
    {
        var products = await repo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(category))
            products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(search))
            products = products.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();

        if (inStockOnly == true)
            products = products.Where(p => p.IsAvailable).ToList();

        return mapper.Map<List<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await repo.GetByIdAsync(id);
        return product is null ? null : mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var product = mapper.Map<Product>(dto);
        await repo.AddAsync(product);
        await repo.SaveChangesAsync();
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await repo.GetByIdAsync(id);
        if (product is null) return null;

        mapper.Map(dto, product);

        await repo.UpdateAsync(product);

        await repo.SaveChangesAsync();
        return mapper.Map<ProductDto>(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await repo.GetByIdAsync(id);
        if (product is null) return false;

        await repo.DeleteAsync(id);
        await repo.SaveChangesAsync();
        return true;
    }
}