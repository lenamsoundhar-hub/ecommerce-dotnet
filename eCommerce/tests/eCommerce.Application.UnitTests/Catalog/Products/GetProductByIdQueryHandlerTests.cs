using eCommerce.Application.Catalog.Products.Queries.GetProductById;
using eCommerce.Application.Common.Exceptions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using NSubstitute;

namespace eCommerce.Application.UnitTests.Catalog.Products;

public class GetProductByIdQueryHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests() => _handler = new GetProductByIdQueryHandler(_products);

    [Fact]
    public async Task Handle_ProjectsTheAggregateOntoTheDto()
    {
        var product = Product.Create(
            "Standing Desk",
            "Electric desk.",
            Sku.From("DSK-STAND-160"),
            Money.From(749.99m, "USD"),
            5);

        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var dto = await _handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        dto.Id.Should().Be(product.Id);
        dto.Sku.Should().Be("DSK-STAND-160");
        dto.Price.Should().Be(749.99m);
        dto.Currency.Should().Be("USD");
        dto.StockQuantity.Should().Be(5);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ThrowsWhenTheProductIsMissing()
    {
        var id = Guid.CreateVersion7();

        _products.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var act = () => _handler.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{id}*");
    }
}
