using Dapper;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using EcommerceApp.Infrastructure.Persistence;

namespace EcommerceApp.Infrastructure.Repositories;

public class ProductRepository(IDbConnectionFactory connectionFactory) : IProductRepository
{
    public async Task<List<Product>> GetAllAsync()
    {
        const string sql = """
            SELECT Id, Name, Description, ImageUrl, Category, Price, StockQuantity, CreatedAt, UpdatedAt
            FROM Products
            ORDER BY Name
            """;

        using var connection = connectionFactory.CreateConnection();
        var products = await connection.QueryAsync<Product>(sql);
        return products.ToList();
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
