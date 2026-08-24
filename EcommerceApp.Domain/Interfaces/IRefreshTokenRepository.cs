using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task RevokeAsync(Guid id, string? replacedByTokenHash);
    Task RevokeAllForUserAsync(Guid userId);
}
