using eCommerce.Application.Catalog.Products.Commands.CreateProduct;
using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Common;
using eCommerce.Domain.ValueObjects;
using NSubstitute;

namespace eCommerce.Application.UnitTests.Catalog.Products;

public class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests() => _handler = new CreateProductCommandHandler(_products, _unitOfWork);

    private static CreateProductCommand AValidCommand() =>
        new("Standing Desk", "Electric desk.", "dsk-stand-160", 749.99m, "usd", 5);

    [Fact]
    public async Task Handle_AddsTheProductAndCommitsOnce()
    {
        _products.ExistsWithSkuAsync(Arg.Any<Sku>(), Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(AValidCommand(), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        _products.Received(1).Add(Arg.Is<Product>(p => p.Id == id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalisesTheSkuAndCurrencyBeforePersisting()
    {
        _products.ExistsWithSkuAsync(Arg.Any<Sku>(), Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(AValidCommand(), CancellationToken.None);

        _products.Received(1).Add(Arg.Is<Product>(p =>
            p.Sku.Value == "DSK-STAND-160" && p.Price.Currency == "USD"));
    }

    [Fact]
    public async Task Handle_RejectsADuplicateSku()
    {
        _products.ExistsWithSkuAsync(Arg.Any<Sku>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _handler.Handle(AValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already exists*");

        _products.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotCommitWhenTheDomainRejectsTheInput()
    {
        _products.ExistsWithSkuAsync(Arg.Any<Sku>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _handler.Handle(AValidCommand() with { Name = "  " }, CancellationToken.None);

        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
