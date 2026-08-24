namespace EcommerceApp.Application.Payments.DTOs;

// Any request that carries raw card details (a direct charge, a cart checkout, ...)
// implements this so the card-number/expiry/CVV validation rules can be shared instead of
// copy-pasted — see PaymentCardValidator<T> in Payments/Validators.
public interface IPaymentCardDetails
{
    string Currency { get; }
    string CardNumber { get; }
    string CardholderName { get; }
    int ExpiryMonth { get; }
    int ExpiryYear { get; }
    string Cvv { get; }
}

public class ChargeRequestDto : IPaymentCardDetails
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CardNumber { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Cvv { get; set; } = string.Empty;
}

public class PaymentResultDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? GatewayReference { get; set; }
    public DateTime CreatedAt { get; set; }
}
