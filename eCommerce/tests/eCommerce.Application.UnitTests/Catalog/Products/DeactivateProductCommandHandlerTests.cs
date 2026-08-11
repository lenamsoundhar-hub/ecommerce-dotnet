using eCommerce.Application.Catalog.Products.Commands.DeactivateProduct;
using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Catalog.Events;
using eCommerce.Domain.Common;
using eCommerce.Domain.ValueObjects;
using NSubstitute;

namespace eCommerce.Application.UnitTests.Catalog.Products;

public class DeactivateProductCommandHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeactivateProductCommandHandler _handler;

    public DeactivateProductCommandHandlerTests() =>
        _handler = new DeactivateProductCommandHandler(_products, _unitOfWork);

    private static Product AProduct() => Product.Create(
        "Standing Desk",
        "Electric desk.",
        Sku.From("DSK-STAND-160"),
        Money.From(749.99m, "USD"),
        5);

    [Fact]
    public async Task Handle_DeactivatesTheProductAndCommits()
    {
        var product = AProduct();
        product.ClearDomainEvents();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(new DeactivateProductCommand(product.Id), CancellationToken.None);

        product.IsActive.Should().BeFalse();
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductDeactivatedDomainEvent>();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Deactivation is a soft delete, so the aggregate is never removed from the
    /// repository — only its flag changes.
    /// </summary>
    [Fact]
    public async Task Handle_DoesNotDeleteTheAggregate()
    {
        var product = AProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(new DeactivateProductCommand(product.Id), CancellationToken.None);

        _products.DidNotReceive().Add(Arg.Any<Product>());
        product.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_IsIdempotentForAnAlreadyDeactivatedProduct()
    {
        var product = AProduct();
        product.Deactivate();
        product.ClearDomainEvents();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(new DeactivateProductCommand(product.Id), CancellationToken.None);

        product.IsActive.Should().BeFalse();
        product.DomainEvents.Should().BeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsAndDoesNotCommitWhenTheProductIsMissing()
    {
        var id = Guid.CreateVersion7();
        _products.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var act = () => _handler.Handle(new DeactivateProductCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{id}*");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
