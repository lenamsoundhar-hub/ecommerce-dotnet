using FluentValidation;
using MediatR;

namespace eCommerce.Application.Catalog.Products.Commands.RestockProduct;

public sealed record RestockProductCommand(Guid ProductId, int Quantity) : IRequest;

public sealed class RestockProductCommandValidator : AbstractValidator<RestockProductCommand>
{
    public RestockProductCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();

        RuleFor(c => c.Quantity).GreaterThan(0);
    }
}
