using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds development data. Called from the Api at
/// start-up in non-production environments.
/// </summary>
public sealed class ApplicationDbContextInitialiser
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;

    public ApplicationDbContextInitialiser(
        ApplicationDbContext context,
        ILogger<ApplicationDbContextInitialiser> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to apply database migrations");
            throw;
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.Products.AddRange(
            Product.Create(
                "Aeron Chair",
                "Ergonomic office chair with lumbar support.",
                Sku.From("CHR-AERON-01"),
                Money.From(1395.00m, "USD"),
                12),
            Product.Create(
                "Standing Desk",
                "Electric height-adjustable desk, 160x80cm.",
                Sku.From("DSK-STAND-160"),
                Money.From(749.99m, "USD"),
                5),
            Product.Create(
                "Mechanical Keyboard",
                "65% hot-swappable keyboard with tactile switches.",
                Sku.From("KBD-MECH-65"),
                Money.From(129.50m, "USD"),
                0));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded the product catalogue with sample data");
    }
}
