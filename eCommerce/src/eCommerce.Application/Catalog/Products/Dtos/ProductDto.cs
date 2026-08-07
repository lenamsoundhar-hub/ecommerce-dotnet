using eCommerce.Domain.Catalog;

namespace eCommerce.Application.Catalog.Products.Dtos;

/// <summary>Read model returned to clients; keeps Domain types off the wire.</summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc)
{
    public static ProductDto FromProduct(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Sku.Value,
        product.Price.Amount,
        product.Price.Currency,
        product.StockQuantity,
        product.IsActive,
        product.CreatedOnUtc,
        product.ModifiedOnUtc);
}
