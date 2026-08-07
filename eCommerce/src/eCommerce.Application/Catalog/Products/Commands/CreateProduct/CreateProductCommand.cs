using MediatR;

namespace eCommerce.Application.Catalog.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity) : IRequest<Guid>;
