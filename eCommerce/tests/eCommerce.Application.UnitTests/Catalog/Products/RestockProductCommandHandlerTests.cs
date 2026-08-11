using eCommerce.Application.Catalog.Products.Commands.RestockProduct;
using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Common;
using eCommerce.Domain.Exceptions;
using eCommerce.Domain.ValueObjects;
using NSubstitute;

namespace eCommerce.Application.UnitTests.Catalog.Products;

public class RestockProductCommandHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RestockProductCommandHandler _handler;

    public RestockProductCommandHandlerTests() =>
        _handler = new RestockProductCommandHandler(_products, _unitOfWork);

    private static Product AProduct(int stock = 5) => Product.Create(
        "Standing Desk",
        "Electric desk.",
        Sku.From("DSK-STAND-160"),
        Money.From(749.99m, "USD"),
        stock);

    [Fact]
    public async Task Handle_AddsToTheStockAndCommits()
    {
        var product = AProduct(stock: 5);
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(new RestockProductCommand(product.Id, 10), CancellationToken.None);

        product.StockQuantity.Should().Be(15);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsAndDoesNotCommitWhenTheProductIsMissing()
    {
        var id = Guid.CreateVersion7();
        _products.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var act = () => _handler.Handle(new RestockProductCommand(id, 10), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{id}*");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotCommitWhenTheProductIsDeactivated()
    {
        var product = AProduct();
        product.Deactivate();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var act = () => _handler.Handle(
            new RestockProductCommand(product.Id, 10),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*deactivated*");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class RestockProductCommandValidatorTests
{
    private readonly RestockProductCommandValidator _validator = new();

    [Fact]
    public void AWellFormedCommandPasses() =>
        _validator.Validate(new RestockProductCommand(Guid.CreateVersion7(), 5))
            .IsValid.Should().BeTrue();

    [Fact]
    public void ProductIdIsRequired()
    {
        var result = _validator.Validate(new RestockProductCommand(Guid.Empty, 5));

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RestockProductCommand.ProductId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void QuantityMustBePositive(int quantity)
    {
        var result = _validator.Validate(new RestockProductCommand(Guid.CreateVersion7(), quantity));

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RestockProductCommand.Quantity));
    }
}
