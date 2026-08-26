using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Domain.Interfaces;

public interface IProductRepository
{
    // Returns just the requested page plus the total row count across all pages (matching the
    // same filters), fetched via COUNT(*) OVER() in a single round trip instead of a separate
    // COUNT query.
    Task<(List<Product> Items, int TotalCount)> GetAllAsync(
        string? category, string? search, bool? inStockOnly, int page, int pageSize);
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product p);
    Task UpdateAsync(Product p);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}

