using eCommerce.Domain.ValueObjects;
using FluentValidation;

namespace eCommerce.Application.Catalog.Products.Commands.UpdateProductPrice;

public sealed class UpdateProductPriceCommandValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceCommandValidator()
    {
        RuleFor(c => c.ProductId)
            .NotEmpty();

        RuleFor(c => c.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.Currency)
            .NotEmpty()
            .Length(Money.CurrencyCodeLength)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("'Currency' must be a 3-letter ISO 4217 code.");
    }
}
