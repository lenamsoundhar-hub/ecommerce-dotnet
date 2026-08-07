using eCommerce.Application.Catalog.Products.Dtos;
using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using MediatR;

namespace eCommerce.Application.Catalog.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _products;

    public GetProductByIdQueryHandler(IProductRepository products) => _products = products;

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        return ProductDto.FromProduct(product);
    }
}
