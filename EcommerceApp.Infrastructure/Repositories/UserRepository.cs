using Dapper;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using EcommerceApp.Infrastructure.Persistence;

namespace EcommerceApp.Infrastructure.Repositories;

public class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT Id, Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedAt, UpdatedAt
            FROM Users
            WHERE Id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = """
            SELECT Id, Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedAt, UpdatedAt
            FROM Users
            WHERE Email = @Email
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM Users WHERE Email = @Email) THEN 1 ELSE 0 END";

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Email = email });
    }

    public async Task AddAsync(User user)
    {
        const string sql = """
            INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @Email, @PasswordHash, @FirstName, @LastName, @Role, @IsActive, @CreatedAt, @UpdatedAt)
            """;

        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }
}
