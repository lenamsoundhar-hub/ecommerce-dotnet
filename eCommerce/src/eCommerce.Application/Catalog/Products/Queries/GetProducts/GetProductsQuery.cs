using eCommerce.Application.Catalog.Products.Dtos;
using eCommerce.Application.Common.Models;
using eCommerce.Domain.Catalog;
using FluentValidation;
using MediatR;

namespace eCommerce.Application.Catalog.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    bool ActiveOnly = true) : IRequest<PagedList<ProductDto>>;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public const int MaxPageSize = 100;

    public GetProductsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0);

        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedList<ProductDto>>
{
    private readonly IProductRepository _products;

    public GetProductsQueryHandler(IProductRepository products) => _products = products;

    public async Task<PagedList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _products.ListAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.ActiveOnly,
            cancellationToken);

        return new PagedList<ProductDto>(
            [.. items.Select(ProductDto.FromProduct)],
            totalCount,
            request.Page,
            request.PageSize);
    }
}
