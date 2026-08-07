using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Proves the Domain's value objects survive a round trip through real SQL —
/// the <see cref="Sku"/> value converter and the <see cref="Money"/> owned type
/// in particular.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ProductMappingTests : IAsyncLifetime
{
    private readonly SqlServerDatabaseFixture _fixture;

    public ProductMappingTests(SqlServerDatabaseFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static Product AProduct(string sku = "DSK-STAND-160", decimal price = 749.99m) =>
        Product.Create(
            "Standing Desk",
            "Electric height-adjustable desk.",
            Sku.From(sku),
            Money.From(price, "USD"),
            5);

    [Fact]
    public async Task AProductRoundTripsWithItsValueObjectsIntact()
    {
        var product = AProduct();

        await using (var write = _fixture.CreateContext())
        {
            write.Products.Add(product);
            await write.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();
        var loaded = await read.Products.SingleAsync(p => p.Id == product.Id);

        loaded.Sku.Should().Be(Sku.From("DSK-STAND-160"));
        loaded.Sku.Value.Should().Be("DSK-STAND-160");
        loaded.Price.Should().Be(Money.From(749.99m, "USD"));
        loaded.Price.Amount.Should().Be(749.99m);
        loaded.Price.Currency.Should().Be("USD");
        loaded.Name.Should().Be("Standing Desk");
        loaded.StockQuantity.Should().Be(5);
        loaded.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task MoneyIsStoredAsTwoColumnsOnTheProductsTableRatherThanAJoin()
    {
        var product = AProduct();

        await using var context = _fixture.CreateContext();
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Reads the flattened columns directly; this only compiles to valid SQL
        // if Money was mapped onto Products rather than into its own table.
        var currency = await context.Database
            .SqlQuery<string>($"SELECT [Currency] AS [Value] FROM [Products] WHERE [Id] = {product.Id}")
            .SingleAsync();

        var amount = await context.Database
            .SqlQuery<decimal>($"SELECT [Price] AS [Value] FROM [Products] WHERE [Id] = {product.Id}")
            .SingleAsync();

        currency.Should().Be("USD");
        amount.Should().Be(749.99m);
    }

    [Fact]
    public async Task SkuIsStoredAsItsNormalisedStringValue()
    {
        var product = AProduct(sku: "dsk-lower-01");

        await using var context = _fixture.CreateContext();
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var stored = await context.Database
            .SqlQuery<string>($"SELECT [Sku] AS [Value] FROM [Products] WHERE [Id] = {product.Id}")
            .SingleAsync();

        stored.Should().Be("DSK-LOWER-01");
    }

    [Fact]
    public async Task ThePriceColumnPreservesTwoDecimalPlacesAtScale()
    {
        var product = AProduct(price: 12345678.99m);

        await using (var write = _fixture.CreateContext())
        {
            write.Products.Add(product);
            await write.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();
        var loaded = await read.Products.SingleAsync(p => p.Id == product.Id);

        loaded.Price.Amount.Should().Be(12345678.99m);
    }

    [Fact]
    public async Task TheUniqueIndexRejectsADuplicateSku()
    {
        await using (var seed = _fixture.CreateContext())
        {
            seed.Products.Add(AProduct(sku: "DUP-1"));
            await seed.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();
        context.Products.Add(AProduct(sku: "DUP-1"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DomainEventsAreNotPersisted()
    {
        var product = AProduct();

        await using var context = _fixture.CreateContext();
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // A mapped DomainEvents column would make this SELECT * fail to project.
        var columns = await context.Database
            .SqlQuery<string>($"""
                SELECT [COLUMN_NAME] AS [Value]
                FROM [INFORMATION_SCHEMA].[COLUMNS]
                WHERE [TABLE_NAME] = 'Products'
                """)
            .ToListAsync();

        columns.Should().NotContain("DomainEvents");
        columns.Should().BeEquivalentTo(
            "Id", "Name", "Description", "Sku", "Price", "Currency",
            "StockQuantity", "IsActive", "CreatedOnUtc", "ModifiedOnUtc");
    }
}
