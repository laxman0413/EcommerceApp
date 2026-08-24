using EcommerceApp.Application.Cart.DTOs;
using EcommerceApp.Application.Payments.Validators;
using FluentValidation;

namespace EcommerceApp.Application.Cart.Validators;

public class AddCartItemValidator : AbstractValidator<AddCartItemDto>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1")
            .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100 per line");
    }
}

public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemDto>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1 — use DELETE to remove the item instead")
            .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100 per line");
    }
}

// Reuses the same card-number/expiry/CVV rules as a direct payment charge — see
// PaymentCardValidator<T> for the shared logic (Luhn check, expiry-in-the-past, etc).
public class CheckoutRequestValidator : AbstractValidator<CheckoutRequestDto>
{
    public CheckoutRequestValidator()
    {
        Include(new PaymentCardValidator<CheckoutRequestDto>());
    }
}
