using Dapper;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using EcommerceApp.Infrastructure.Persistence;

namespace EcommerceApp.Infrastructure.Repositories;

public class CartRepository(IDbConnectionFactory connectionFactory) : ICartRepository
{
    public async Task<List<CartItem>> GetByUserIdAsync(Guid userId)
    {
        const string sql = """
            SELECT Id, UserId, ProductId, Quantity, CreatedAt, UpdatedAt
            FROM CartItems
            WHERE UserId = @UserId
            """;

        using var connection = connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<CartItem>(sql, new { UserId = userId });
        return items.ToList();
    }

    public async Task<List<CartItemDetail>> GetDetailedByUserIdAsync(Guid userId)
    {
        const string sql = """
            SELECT ci.ProductId, p.Name AS ProductName, p.Price AS UnitPrice, ci.Quantity, p.StockQuantity
            FROM CartItems ci
            JOIN Products p ON p.Id = ci.ProductId
            WHERE ci.UserId = @UserId
            """;

        using var connection = connectionFactory.CreateConnection();
        var details = await connection.QueryAsync<CartItemDetail>(sql, new { UserId = userId });
        return details.ToList();
    }

    public async Task<CartItem?> GetItemAsync(Guid userId, Guid productId)
    {
        const string sql = """
            SELECT Id, UserId, ProductId, Quantity, CreatedAt, UpdatedAt
            FROM CartItems
            WHERE UserId = @UserId AND ProductId = @ProductId
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CartItem>(sql, new { UserId = userId, ProductId = productId });
    }

    public async Task AddAsync(CartItem item)
    {
        const string sql = """
            INSERT INTO CartItems (Id, UserId, ProductId, Quantity, CreatedAt, UpdatedAt)
            VALUES (@Id, @UserId, @ProductId, @Quantity, @CreatedAt, @UpdatedAt)
            """;

        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, item);
    }

    public async Task UpdateQuantityAsync(Guid id, int quantity)
    {
        const string sql = """
            UPDATE CartItems
            SET Quantity = @Quantity, UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Quantity = quantity, UpdatedAt = DateTime.UtcNow });
    }

    public async Task RemoveAsync(Guid id)
    {
        const string sql = "DELETE FROM CartItems WHERE Id = @Id";

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task ClearAsync(Guid userId)
    {
        const string sql = "DELETE FROM CartItems WHERE UserId = @UserId";

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
