namespace EcommerceApp.Domain.Entities;

// Not a persisted table — this is the shape of ICartRepository's joined read query
// (CartItems JOIN Products), used to build the cart response and to re-validate price/stock
// at checkout without a per-line round trip to the product table.
public class CartItemDetail
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }
}
