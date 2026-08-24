using EcommerceApp.Domain.Enums;

namespace EcommerceApp.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CardLast4 { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }

    // Set for both successful and declined attempts — the mock gateway always
    // returns a reference; only a hard gateway error (never reached the processor) leaves it null.
    public string? GatewayReference { get; set; }

    // Populated on Declined (business reason, e.g. "card_declined") or Failed (transport/gateway reason).
    public string? FailureReason { get; set; }
}
