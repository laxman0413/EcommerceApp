using Dapper;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Interfaces;
using EcommerceApp.Infrastructure.Persistence;

namespace EcommerceApp.Infrastructure.Repositories;

public class PaymentRepository(IDbConnectionFactory connectionFactory) : IPaymentRepository
{
    public async Task AddAsync(Payment payment)
    {
        const string sql = """
            INSERT INTO Payments (Id, UserId, Amount, Currency, CardLast4, Status, GatewayReference, FailureReason, CreatedAt, UpdatedAt)
            VALUES (@Id, @UserId, @Amount, @Currency, @CardLast4, @Status, @GatewayReference, @FailureReason, @CreatedAt, @UpdatedAt)
            """;

        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, payment);
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT Id, UserId, Amount, Currency, CardLast4, Status, GatewayReference, FailureReason, CreatedAt, UpdatedAt
            FROM Payments
            WHERE Id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Payment>(sql, new { Id = id });
    }
}
