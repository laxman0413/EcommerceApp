namespace EcommerceApp.Application.Payments.Gateway;

// Stand-in for a real provider's SDK (Stripe, Braintree, etc). Application code talks only to
// this interface, so swapping the mock for a real gateway later is an Infrastructure-only change.
public interface IPaymentGateway
{
    Task<GatewayChargeResult> ChargeAsync(GatewayChargeRequest request);
}

public record GatewayChargeRequest(
    decimal Amount,
    string Currency,
    string CardNumber,
    string CardholderName,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv);

public enum GatewayChargeStatus
{
    Succeeded,
    Declined
}

public record GatewayChargeResult(GatewayChargeStatus Status, string GatewayReference, string? DeclineReason);

// Thrown when the "call" to the 3rd party itself fails — timeout, processor outage, malformed
// response — as opposed to the processor successfully declining the card. Callers should treat
// this as transient/infrastructure, not a business outcome.
public class PaymentGatewayCommunicationException(string message) : Exception(message);
