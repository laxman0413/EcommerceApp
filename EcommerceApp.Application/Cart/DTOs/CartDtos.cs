using EcommerceApp.Application.Payments.DTOs;

namespace EcommerceApp.Application.Cart.DTOs;

// One line of the cart response. UnitPrice/ProductName are read live from the product
// catalog on every request — the cart itself only stores ProductId + Quantity — so a price
// change is reflected immediately instead of showing a stale snapshot.
public class CartItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = [];
    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}

public class AddCartItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

// Card details for "pay for whatever is currently in my cart". Deliberately has no Amount
// field — CartService computes that from the cart itself so a client can never charge itself
// (or someone else) an arbitrary amount by tampering with the request body.
public class CheckoutRequestDto : IPaymentCardDetails
{
    public string Currency { get; set; } = "USD";
    public string CardNumber { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Cvv { get; set; } = string.Empty;
}
