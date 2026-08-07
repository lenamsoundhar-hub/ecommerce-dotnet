using MediatR;

namespace eCommerce.Application.Catalog.Products.Commands.UpdateProductPrice;

public sealed record UpdateProductPriceCommand(
    Guid ProductId,
    decimal Price,
    string Currency) : IRequest;
