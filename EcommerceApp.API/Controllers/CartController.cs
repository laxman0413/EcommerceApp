using System.Security.Claims;
using EcommerceApp.Application.Cart.DTOs;
using EcommerceApp.Application.Cart.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.API.Controllers;

// Every endpoint here operates on "my cart" — there is no route parameter for the cart
// itself, the current user's Id (from the JWT) is the only identifier needed.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController(
    ICartService cartService,
    IValidator<AddCartItemDto> addValidator,
    IValidator<UpdateCartItemDto> updateValidator,
    IValidator<CheckoutRequestDto> checkoutValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCart()
        => Ok(await cartService.GetCartAsync(GetUserId()));

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemDto dto)
    {
        var validation = await addValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        return Ok(await cartService.AddItemAsync(GetUserId(), dto));
    }

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid productId, [FromBody] UpdateCartItemDto dto)
    {
        var validation = await updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        return Ok(await cartService.UpdateItemAsync(GetUserId(), productId, dto));
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId)
        => Ok(await cartService.RemoveItemAsync(GetUserId(), productId));

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto dto)
    {
        var validation = await checkoutValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        // Amount is never taken from the client — CartService computes it from the cart's
        // current contents, then charges through the same IPaymentService as a direct charge.
        // A decline (402) or gateway failure (502) leaves the cart untouched for a retry.
        var result = await cartService.CheckoutAsync(GetUserId(), dto);
        return Ok(result);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
