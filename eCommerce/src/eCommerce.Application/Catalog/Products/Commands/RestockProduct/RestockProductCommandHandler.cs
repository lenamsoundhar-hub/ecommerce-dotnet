using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Common;
using MediatR;

namespace eCommerce.Application.Catalog.Products.Commands.RestockProduct;

public sealed class RestockProductCommandHandler : IRequestHandler<RestockProductCommand>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public RestockProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RestockProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Restock(request.Quantity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
