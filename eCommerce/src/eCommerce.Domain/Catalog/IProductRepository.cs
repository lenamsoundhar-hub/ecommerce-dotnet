using eCommerce.Domain.ValueObjects;

namespace eCommerce.Domain.Catalog;

/// <summary>
/// Collection-like access to the <see cref="Product"/> aggregate. Implemented in
/// the Infrastructure layer; the Domain only states what it needs.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithSkuAsync(Sku sku, CancellationToken cancellationToken = default);

    /// <summary>Returns one page of products alongside the unpaged total.</summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    void Add(Product product);

    // There is deliberately no Remove. Retiring a product is a soft delete via
    // Product.Deactivate(), so that order history keeps referring to a row that
    // still exists. Add a purpose-named method here if a hard delete is ever
    // genuinely required (e.g. erasure requests).
}
