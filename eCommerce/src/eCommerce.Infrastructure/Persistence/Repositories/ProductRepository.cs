using System.Linq.Expressions;
using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        _context.Products.FirstOrDefaultAsync(HasSku(sku), cancellationToken);

    public Task<bool> ExistsWithSkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        _context.Products.AnyAsync(HasSku(sku), cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(Product product) => _context.Products.Add(product);

    /// <summary>
    /// Compares against the <see cref="Sku"/> itself so EF applies the value
    /// converter to the parameter. Comparing to <c>sku.Value</c> instead makes
    /// EF try to convert the string parameter back into a <see cref="Sku"/> and
    /// throw at execution time.
    /// </summary>
    private static Expression<Func<Product, bool>> HasSku(Sku sku) =>
        product => product.Sku == sku;
}
