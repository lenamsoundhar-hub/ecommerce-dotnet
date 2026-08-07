using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Common;
using eCommerce.Domain.ValueObjects;
using MediatR;

namespace eCommerce.Application.Catalog.Products.Commands.UpdateProductPrice;

public sealed class UpdateProductPriceCommandHandler : IRequestHandler<UpdateProductPriceCommand>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductPriceCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.ChangePrice(Money.From(request.Price, request.Currency));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
