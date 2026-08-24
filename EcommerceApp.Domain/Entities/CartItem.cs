namespace EcommerceApp.Domain.Entities;

// One row per (user, product). There is no separate "Cart" header row — a user's cart is
// simply "all CartItems where UserId = them". Adding a product already in the cart increases
// Quantity on the existing row instead of inserting a duplicate.
public class CartItem : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
