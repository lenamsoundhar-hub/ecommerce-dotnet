using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using FluentValidation;

namespace eCommerce.Application.Catalog.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(Product.NameMaxLength);

        RuleFor(c => c.Description)
            .MaximumLength(Product.DescriptionMaxLength);

        RuleFor(c => c.Sku)
            .NotEmpty()
            .MaximumLength(Sku.MaxLength)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("'Sku' may contain only letters, digits, hyphens and underscores.");

        RuleFor(c => c.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.Currency)
            .NotEmpty()
            .Length(Money.CurrencyCodeLength)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("'Currency' must be a 3-letter ISO 4217 code.");

        RuleFor(c => c.StockQuantity)
            .GreaterThanOrEqualTo(0);
    }
}
