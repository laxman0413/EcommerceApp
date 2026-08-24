using Dapper;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using EcommerceApp.Infrastructure.Persistence;

namespace EcommerceApp.Infrastructure.Repositories;

public class RefreshTokenRepository(IDbConnectionFactory connectionFactory) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token)
    {
        const string sql = """
            INSERT INTO RefreshTokens (Id, UserId, TokenHash, ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @CreatedAt, @RevokedAt, @ReplacedByTokenHash)
            """;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, token);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        const string sql = """
            SELECT Id, UserId, TokenHash, ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash
            FROM RefreshTokens
            WHERE TokenHash = @TokenHash
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { TokenHash = tokenHash });
    }

    public async Task RevokeAsync(Guid id, string? replacedByTokenHash)
    {
        const string sql = """
            UPDATE RefreshTokens
            SET RevokedAt = @RevokedAt, ReplacedByTokenHash = @ReplacedByTokenHash
            WHERE Id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, RevokedAt = DateTime.UtcNow, ReplacedByTokenHash = replacedByTokenHash });
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        const string sql = """
            UPDATE RefreshTokens
            SET RevokedAt = @RevokedAt
            WHERE UserId = @UserId AND RevokedAt IS NULL
            """;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, RevokedAt = DateTime.UtcNow });
    }
}
