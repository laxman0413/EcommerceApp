using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Domain.Interfaces;

public interface ICartRepository
{
    Task<List<CartItem>> GetByUserIdAsync(Guid userId);

    // Same rows as GetByUserIdAsync, joined against Products in one query so callers building
    // a response (or totaling a checkout) don't fetch each product one at a time.
    Task<List<CartItemDetail>> GetDetailedByUserIdAsync(Guid userId);
    Task<CartItem?> GetItemAsync(Guid userId, Guid productId);
    Task AddAsync(CartItem item);
    Task UpdateQuantityAsync(Guid id, int quantity);
    Task RemoveAsync(Guid id);

    // Wipes the whole cart in one statement — used right after a successful checkout.
    Task ClearAsync(Guid userId);
}
