using eCommerce.Application.Catalog.Products.Queries.GetProducts;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using NSubstitute;

namespace eCommerce.Application.UnitTests.Catalog.Products;

public class GetProductsQueryHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests() => _handler = new GetProductsQueryHandler(_products);

    private static Product AProduct(string name, string sku) =>
        Product.Create(name, "A description.", Sku.From(sku), Money.From(749.99m, "USD"), 5);

    private void RepositoryReturns(IReadOnlyList<Product> items, int totalCount) =>
        _products.ListAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns((items, totalCount));

    [Fact]
    public async Task Handle_ProjectsEachAggregateOntoADto()
    {
        RepositoryReturns([AProduct("Standing Desk", "DSK-1")], totalCount: 1);

        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        var dto = result.Items.Should().ContainSingle().Subject;
        dto.Name.Should().Be("Standing Desk");
        dto.Sku.Should().Be("DSK-1");
        dto.Price.Should().Be(749.99m);
        dto.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_PassesThePagingAndFilterArgumentsThrough()
    {
        RepositoryReturns([], totalCount: 0);

        await _handler.Handle(
            new GetProductsQuery(Page: 3, PageSize: 15, SearchTerm: "desk", ActiveOnly: false),
            CancellationToken.None);

        await _products.Received(1).ListAsync(3, 15, "desk", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesTheDocumentedDefaultsWhenTheCallerSuppliesNothing()
    {
        RepositoryReturns([], totalCount: 0);

        await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        await _products.Received(1).ListAsync(1, 20, null, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComputesThePagingMetadataFromTheUnpagedTotal()
    {
        RepositoryReturns([AProduct("A", "A-1"), AProduct("B", "B-1")], totalCount: 5);

        var result = await _handler.Handle(
            new GetProductsQuery(Page: 2, PageSize: 2),
            CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReportsNoAdjacentPagesOnASinglePageOfResults()
    {
        RepositoryReturns([AProduct("A", "A-1")], totalCount: 1);

        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyPageRatherThanNull()
    {
        RepositoryReturns([], totalCount: 0);

        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }
}

public class GetProductsQueryValidatorTests
{
    private readonly GetProductsQueryValidator _validator = new();

    [Fact]
    public void TheDefaultQueryPasses() =>
        _validator.Validate(new GetProductsQuery()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageMustBePositive(int page)
    {
        var result = _validator.Validate(new GetProductsQuery(Page: page));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetProductsQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetProductsQueryValidator.MaxPageSize + 1)]
    public void PageSizeMustBeWithinTheAllowedRange(int pageSize)
    {
        var result = _validator.Validate(new GetProductsQuery(PageSize: pageSize));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetProductsQuery.PageSize));
    }

    [Fact]
    public void TheMaximumPageSizeItselfIsAllowed() =>
        _validator.Validate(new GetProductsQuery(PageSize: GetProductsQueryValidator.MaxPageSize))
            .IsValid.Should().BeTrue();
}
