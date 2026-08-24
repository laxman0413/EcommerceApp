using AutoMapper;
using EcommerceApp.Application.Common.Exceptions;
using EcommerceApp.Application.Payments.DTOs;
using EcommerceApp.Application.Payments.Gateway;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Enums;
using EcommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcommerceApp.Application.Payments.Services;

public class PaymentService(
    IPaymentGateway paymentGateway,
    IPaymentRepository paymentRepository,
    IMapper mapper,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentResultDto> ChargeAsync(ChargeRequestDto dto, Guid userId)
    {
        var currency = dto.Currency.Trim().ToUpperInvariant();
        var cardNumber = dto.CardNumber.Replace(" ", string.Empty);

        var payment = new Payment
        {
            UserId = userId,
            Amount = dto.Amount,
            Currency = currency,
            CardLast4 = cardNumber[^4..]
        };

        var gatewayRequest = new GatewayChargeRequest(
            dto.Amount,
            currency,
            cardNumber,
            dto.CardholderName,
            dto.ExpiryMonth,
            dto.ExpiryYear,
            dto.Cvv);

        try
        {
            var result = await paymentGateway.ChargeAsync(gatewayRequest);
            payment.GatewayReference = result.GatewayReference;

            if (result.Status == GatewayChargeStatus.Declined)
            {
                payment.Status = PaymentStatus.Declined;
                payment.FailureReason = result.DeclineReason;
                await paymentRepository.AddAsync(payment);

                logger.LogWarning(
                    "Payment {PaymentId} for user {UserId} was declined: {Reason}",
                    payment.Id, userId, result.DeclineReason);
                throw new PaymentDeclinedAppException(result.DeclineReason ?? "The payment was declined");
            }

            payment.Status = PaymentStatus.Succeeded;
            await paymentRepository.AddAsync(payment);

            logger.LogInformation("Payment {PaymentId} for user {UserId} succeeded", payment.Id, userId);
            return mapper.Map<PaymentResultDto>(payment);
        }
        catch (PaymentGatewayCommunicationException ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = ex.Message;
            await paymentRepository.AddAsync(payment);

            logger.LogError(ex, "Payment {PaymentId} for user {UserId} failed to reach the gateway", payment.Id, userId);
            throw new PaymentGatewayAppException("Payment could not be processed right now. Please try again.");
        }
    }
}
