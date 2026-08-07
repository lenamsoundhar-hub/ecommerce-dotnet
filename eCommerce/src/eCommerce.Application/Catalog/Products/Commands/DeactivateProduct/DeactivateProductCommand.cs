using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Common;
using MediatR;

namespace eCommerce.Application.Catalog.Products.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest;

public sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
