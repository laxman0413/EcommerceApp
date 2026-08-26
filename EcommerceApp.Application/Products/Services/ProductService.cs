using AutoMapper;
using EcommerceApp.Application.Common.DTOs;
using EcommerceApp.Application.Products.DTOs;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;

namespace EcommerceApp.Application.Products.Services;

public class ProductService(IProductRepository repo, IMapper mapper) : IProductService
{
    private const int MaxPageSize = 100;

    public async Task<PagedResultDto<ProductDto>> GetAllAsync(string? category, string? search, bool? inStockOnly, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (products, totalCount) = await repo.GetAllAsync(category, search, inStockOnly, page, pageSize);

        return new PagedResultDto<ProductDto>
        {
            Items = mapper.Map<List<ProductDto>>(products),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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