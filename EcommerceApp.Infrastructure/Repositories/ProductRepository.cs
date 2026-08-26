using AutoMapper;
using Dapper;
using EcommerceApp.Application.Products.DTOs;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using EcommerceApp.Infrastructure.Persistence;

namespace EcommerceApp.Infrastructure.Repositories;

public class ProductRepository(IDbConnectionFactory connectionFactory) : IProductRepository
{
    public async Task<(List<Product> Items, int TotalCount)> GetAllAsync(
        string? category,
        string? search,
        bool? inStockOnly,
        int page,
        int pageSize)
    {
        const string sql = """
                SELECT
                    Id, Name, Description, ImageUrl,
                    Category, Price, StockQuantity,
                    CreatedAt, UpdatedAt,
                    COUNT(*) OVER() AS TotalCount
                FROM Products
                WHERE
                    (@Category   IS NULL OR LOWER(Category)  LIKE '%' + @Category + '%')
                    AND (@Search IS NULL OR LOWER(Name) LIKE '%' + @Search + '%'
                                         OR LOWER(Description) LIKE '%' + @Search + '%')
                    AND (@InStockOnly IS NULL OR StockQuantity > 0)
                ORDER BY Name
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                """;

        using var connection = connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<ProductRow>(sql, new
        {
            Category = string.IsNullOrWhiteSpace(category) ? null : category.ToLower(),
            Search = string.IsNullOrWhiteSpace(search) ? null : search.ToLower(),
            InStockOnly = inStockOnly == true ? (int?)1 : null,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        })).ToList();

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        var items = rows.Cast<Product>().ToList();
        return (items, totalCount);
    }

    // COUNT(*) OVER() rides along on every row of the page, so Dapper needs a Product-shaped
    // type with that extra column to bind into. Never returned from the repository itself.
    private class ProductRow : Product
    {
        public int TotalCount { get; set; }
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT Id, Name, Description, ImageUrl, Category, Price, StockQuantity, CreatedAt, UpdatedAt
            FROM Products
            WHERE Id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task AddAsync(Product product)
    {
        const string sql = """
            INSERT INTO Products (Id, Name, Description, ImageUrl, Category, Price, StockQuantity, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @Description, @ImageUrl, @Category, @Price, @StockQuantity, @CreatedAt, @UpdatedAt)
            """;

        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, product);
    }

    public async Task UpdateAsync(Product product)
    {
        const string sql = """
            UPDATE Products
            SET Name = @Name,
                Description = @Description,
                ImageUrl = @ImageUrl,
                Category = @Category,
                Price = @Price,
                StockQuantity = @StockQuantity,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        product.UpdatedAt = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, product);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM Products WHERE Id = @Id";

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}
