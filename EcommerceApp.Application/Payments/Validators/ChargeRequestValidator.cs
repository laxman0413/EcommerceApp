using EcommerceApp.Application.Payments.DTOs;
using FluentValidation;

namespace EcommerceApp.Application.Payments.Validators;

// Card-number/expiry/CVV/currency rules shared by every request that carries raw card
// details. Concrete validators pull this in with Include(new PaymentCardValidator<T>())
// instead of re-implementing the Luhn check — see CheckoutRequestValidator in Cart/Validators
// for the other place this is used.
public class PaymentCardValidator<T> : AbstractValidator<T> where T : IPaymentCardDetails
{
    public PaymentCardValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency must be a 3-letter ISO code (e.g. USD)");

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Card number is required")
            .Must(BeAPlausibleCardNumber).WithMessage("Card number is not valid")
            .Must(PassLuhnCheck).WithMessage("Card number is not valid")
            .When(x => !string.IsNullOrWhiteSpace(x.CardNumber));

        RuleFor(x => x.CardholderName)
            .NotEmpty().WithMessage("Cardholder name is required")
            .MaximumLength(200).WithMessage("Cardholder name cannot exceed 200 characters");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12).WithMessage("Expiry month must be between 1 and 12");

        RuleFor(x => x.Cvv)
            .NotEmpty().WithMessage("CVV is required")
            .Matches("^[0-9]{3,4}$").WithMessage("CVV must be 3 or 4 digits");

        RuleFor(x => x)
            .Must(x => new DateOnly(x.ExpiryYear < 100 ? 2000 + x.ExpiryYear : x.ExpiryYear, Math.Clamp(x.ExpiryMonth, 1, 12), 1)
                .AddMonths(1) > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Card has expired")
            .When(x => x.ExpiryMonth is >= 1 and <= 12);
    }

    private static bool BeAPlausibleCardNumber(string cardNumber)
    {
        var digitsOnly = cardNumber.Replace(" ", string.Empty);
        return digitsOnly.Length is >= 13 and <= 19 && digitsOnly.All(char.IsDigit);
    }

    // Standard Luhn checksum — catches typos, not fraud. The mock gateway's magic numbers
    // are all valid Luhn numbers so they still pass this check.
    private static bool PassLuhnCheck(string cardNumber)
    {
        var digitsOnly = cardNumber.Replace(" ", string.Empty);
        if (!digitsOnly.All(char.IsDigit))
            return false;

        var sum = 0;
        var alternate = false;
        for (var i = digitsOnly.Length - 1; i >= 0; i--)
        {
            var digit = digitsOnly[i] - '0';
            if (alternate)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}

public class ChargeRequestValidator : AbstractValidator<ChargeRequestDto>
{
    public ChargeRequestValidator()
    {
        Include(new PaymentCardValidator<ChargeRequestDto>());

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1_000_000).WithMessage("Amount exceeds the maximum allowed charge");
    }
}
