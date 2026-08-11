using eCommerce.Application.Catalog.Products.Commands.UpdateProductPrice;
using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.Common;
using eCommerce.Domain.Exceptions;
using eCommerce.Domain.ValueObjects;
using NSubstitute;

namespace eCommerce.Application.UnitTests.Catalog.Products;

public class UpdateProductPriceCommandHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateProductPriceCommandHandler _handler;

    public UpdateProductPriceCommandHandlerTests() =>
        _handler = new UpdateProductPriceCommandHandler(_products, _unitOfWork);

    private static Product AProduct() => Product.Create(
        "Standing Desk",
        "Electric desk.",
        Sku.From("DSK-STAND-160"),
        Money.From(749.99m, "USD"),
        5);

    [Fact]
    public async Task Handle_AppliesTheNewPriceAndCommits()
    {
        var product = AProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(
            new UpdateProductPriceCommand(product.Id, 699.00m, "USD"),
            CancellationToken.None);

        product.Price.Amount.Should().Be(699.00m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalisesTheCurrencyBeforeApplyingIt()
    {
        var product = AProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(
            new UpdateProductPriceCommand(product.Id, 699.00m, "usd"),
            CancellationToken.None);

        product.Price.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_ThrowsAndDoesNotCommitWhenTheProductIsMissing()
    {
        var id = Guid.CreateVersion7();
        _products.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var act = () => _handler.Handle(
            new UpdateProductPriceCommand(id, 10m, "USD"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{id}*");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotCommitWhenTheDomainRejectsACurrencySwitch()
    {
        var product = AProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var act = () => _handler.Handle(
            new UpdateProductPriceCommand(product.Id, 699.00m, "EUR"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*USD*EUR*");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StillCommitsWhenThePriceIsUnchanged()
    {
        var product = AProduct();
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(
            new UpdateProductPriceCommand(product.Id, 749.99m, "USD"),
            CancellationToken.None);

        // The aggregate treats this as a no-op, but the handler is not the place
        // to decide that; committing an empty change set is harmless.
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UpdateProductPriceCommandValidatorTests
{
    private readonly UpdateProductPriceCommandValidator _validator = new();

    private static UpdateProductPriceCommand AValidCommand() =>
        new(Guid.CreateVersion7(), 699.00m, "USD");

    [Fact]
    public void AWellFormedCommandPasses() =>
        _validator.Validate(AValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void ProductIdIsRequired()
    {
        var result = _validator.Validate(AValidCommand() with { ProductId = Guid.Empty });

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateProductPriceCommand.ProductId));
    }

    [Fact]
    public void PriceCannotBeNegative()
    {
        var result = _validator.Validate(AValidCommand() with { Price = -0.01m });

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateProductPriceCommand.Price));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    [InlineData("US1")]
    [InlineData("")]
    public void CurrencyMustBeAThreeLetterCode(string currency)
    {
        var result = _validator.Validate(AValidCommand() with { Currency = currency });

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateProductPriceCommand.Currency));
    }
}
