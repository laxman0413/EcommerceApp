using EcommerceApp.Application.Payments.Gateway;
using Microsoft.Extensions.Logging;

namespace EcommerceApp.Infrastructure.Payments;

//   4242 4242 4242 4242  -> always succeeds
//   4000 0000 0000 0002  -> always declined by the "processor"
//   4000 0000 0000 0119  -> simulates the processor itself being unreachable
//   anything else        -> succeeds, so the happy path doesn't require memorizing magic numbers
public class MockPaymentGateway(ILogger<MockPaymentGateway> logger) : IPaymentGateway
{
    private const string DeclinedCard = "4000000000000002";
    private const string ProcessorErrorCard = "4000000000000119";

    public async Task<GatewayChargeResult> ChargeAsync(GatewayChargeRequest request)
    {
        // Simulate real network latency instead of resolving instantly.
        await Task.Delay(150);

        var cardNumber = request.CardNumber.Replace(" ", string.Empty);

        if (cardNumber == ProcessorErrorCard)
        {
            logger.LogWarning("Mock gateway simulating a processor outage for this charge");
            throw new PaymentGatewayCommunicationException("The payment processor did not respond in time");
        }

        if (cardNumber == DeclinedCard)
        {
            return new GatewayChargeResult(
                GatewayChargeStatus.Declined,
                GatewayReference: $"mock_{Guid.NewGuid():N}",
                DeclineReason: "card_declined: the card issuer declined this charge");
        }

        return new GatewayChargeResult(
            GatewayChargeStatus.Succeeded,
            GatewayReference: $"mock_{Guid.NewGuid():N}",
            DeclineReason: null);
    }
}
