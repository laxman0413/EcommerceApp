using AutoMapper;
using EcommerceApp.Application.Common.Exceptions;
using EcommerceApp.Application.Payments.DTOs;
using EcommerceApp.Application.Payments.Gateway;
using EcommerceApp.Application.Payments.Services;
using EcommerceApp.Application.Tests.TestSupport;
using EcommerceApp.Domain.Entities;
using EcommerceApp.Domain.Enums;
using EcommerceApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceApp.Application.Tests.Payments;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly IMapper _mapper = MapperFactory.Create();
    private readonly PaymentService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public PaymentServiceTests()
    {
        _sut = new PaymentService(
            _gateway.Object,
            _paymentRepository.Object,
            _mapper,
            new Mock<ILogger<PaymentService>>().Object);
    }

    private static ChargeRequestDto ValidRequest(decimal amount = 100m) => new()
    {
        Amount = amount,
        Currency = "usd",
        CardNumber = "4242 4242 4242 4242",
        CardholderName = "Jane Doe",
        ExpiryMonth = 12,
        ExpiryYear = 2030,
        Cvv = "123"
    };

    [Fact]
    public async Task ChargeAsync_NormalizesCurrencyAndStripsSpacesFromCardNumberBeforeCallingGateway()
    {
        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<GatewayChargeRequest>()))
            .ReturnsAsync(new GatewayChargeResult(GatewayChargeStatus.Succeeded, "ref-1", null));

        await _sut.ChargeAsync(ValidRequest(), _userId);

        _gateway.Verify(g => g.ChargeAsync(It.Is<GatewayChargeRequest>(r =>
            r.CardNumber == "4242424242424242" && r.Currency == "USD")), Times.Once);
    }

    [Fact]
    public async Task ChargeAsync_Succeeded_SavesSucceededPaymentWithCardLast4AndReturnsResult()
    {
        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<GatewayChargeRequest>()))
            .ReturnsAsync(new GatewayChargeResult(GatewayChargeStatus.Succeeded, "ref-123", null));

        var result = await _sut.ChargeAsync(ValidRequest(50m), _userId);

        _paymentRepository.Verify(r => r.AddAsync(It.Is<Payment>(p =>
            p.UserId == _userId &&
            p.Amount == 50m &&
            p.Currency == "USD" &&
            p.CardLast4 == "4242" &&
            p.Status == PaymentStatus.Succeeded &&
            p.GatewayReference == "ref-123")), Times.Once);

        Assert.Equal("Succeeded", result.Status);
        Assert.Equal(50m, result.Amount);
        Assert.Equal("ref-123", result.GatewayReference);
    }

    [Fact]
    public async Task ChargeAsync_Declined_SavesDeclinedPaymentAndThrowsPaymentDeclined()
    {
        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<GatewayChargeRequest>()))
            .ReturnsAsync(new GatewayChargeResult(GatewayChargeStatus.Declined, "ref-456", "insufficient_funds"));

        var ex = await Assert.ThrowsAsync<PaymentDeclinedAppException>(
            () => _sut.ChargeAsync(ValidRequest(), _userId));

        Assert.Equal("insufficient_funds", ex.Message);
        _paymentRepository.Verify(r => r.AddAsync(It.Is<Payment>(p =>
            p.Status == PaymentStatus.Declined &&
            p.FailureReason == "insufficient_funds" &&
            p.GatewayReference == "ref-456")), Times.Once);
    }

    [Fact]
    public async Task ChargeAsync_Declined_WithoutReason_UsesFallbackMessage()
    {
        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<GatewayChargeRequest>()))
            .ReturnsAsync(new GatewayChargeResult(GatewayChargeStatus.Declined, "ref-789", null));

        var ex = await Assert.ThrowsAsync<PaymentDeclinedAppException>(
            () => _sut.ChargeAsync(ValidRequest(), _userId));

        Assert.Equal("The payment was declined", ex.Message);
    }

    [Fact]
    public async Task ChargeAsync_GatewayUnreachable_SavesFailedPaymentAndThrowsGatewayException()
    {
        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<GatewayChargeRequest>()))
            .ThrowsAsync(new PaymentGatewayCommunicationException("timed out"));

        var ex = await Assert.ThrowsAsync<PaymentGatewayAppException>(
            () => _sut.ChargeAsync(ValidRequest(), _userId));

        Assert.Equal("Payment could not be processed right now. Please try again.", ex.Message);
        _paymentRepository.Verify(r => r.AddAsync(It.Is<Payment>(p =>
            p.Status == PaymentStatus.Failed &&
            p.FailureReason == "timed out" &&
            p.GatewayReference == null)), Times.Once);
    }
}
