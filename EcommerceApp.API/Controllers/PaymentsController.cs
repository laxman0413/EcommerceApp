using System.Security.Claims;
using EcommerceApp.Application.Payments.DTOs;
using EcommerceApp.Application.Payments.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.API.Controllers;

// Direct "charge this exact amount" endpoint — the caller states the Amount explicitly.
// For "pay for whatever is in my cart", see CartController.Checkout instead, which computes
// the amount server-side and calls the same IPaymentService under the hood.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController(IPaymentService paymentService, IValidator<ChargeRequestDto> validator) : ControllerBase
{
    [HttpPost("charge")]
    public async Task<IActionResult> Charge([FromBody] ChargeRequestDto dto)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Declined/gateway-error outcomes are thrown as AppExceptions and translated to
        // 402/502 by ExceptionHandlingMiddleware — nothing to catch here.
        var result = await paymentService.ChargeAsync(dto, userId);
        return Ok(result);
    }
}
