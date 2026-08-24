using EcommerceApp.Application.Payments.DTOs;

namespace EcommerceApp.Application.Payments.Services;

public interface IPaymentService
{
    Task<PaymentResultDto> ChargeAsync(ChargeRequestDto dto, Guid userId);
}
