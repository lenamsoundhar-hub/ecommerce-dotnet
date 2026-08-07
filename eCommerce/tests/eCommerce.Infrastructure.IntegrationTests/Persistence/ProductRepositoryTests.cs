using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using eCommerce.Infrastructure.Persistence;
using eCommerce.Infrastructure.Persistence.Repositories;

namespace eCommerce.Infrastructure.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public class ProductRepositoryTests : IAsyncLifetime
{
    private readonly SqlServerDatabaseFixture _fixture;

    public ProductRepositoryTests(SqlServerDatabaseFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static Product AProduct(
        string name = "Standing Desk",
        string sku = "DSK-STAND-160",
        int stock = 5) =>
        Product.Create(name, "A description.", Sku.From(sku), Money.From(749.99m, "USD"), stock);

    private async Task SeedAsync(params Product[] products)
    {
        await using var context = _fixture.CreateContext();

        context.Products.AddRange(products);

        await context.SaveChangesAsync();
    }

    private (ApplicationDbContext Context, ProductRepository Repository) NewRepository()
    {
        var context = _fixture.CreateContext();

        return (context, new ProductRepository(context));
    }

    // --- SKU lookups -------------------------------------------------------
    //
    // These are the regression tests for the value-converter translation bug.
    // Handler-level unit tests substitute IProductRepository, so they exercise
    // none of this; only a real relational query does.

    [Fact]
    public async Task ExistsWithSkuAsync_TranslatesToSqlAndFindsAMatch()
    {
        await SeedAsync(AProduct(sku: "DSK-STAND-160"));

        var (context, repository) = NewRepository();
        await using var _ = context;

        var exists = await repository.ExistsWithSkuAsync(Sku.From("DSK-STAND-160"));

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithSkuAsync_ReturnsFalseForAnUnknownSku()
    {
        await SeedAsync(AProduct(sku: "DSK-STAND-160"));

        var (context, repository) = NewRepository();
        await using var _ = context;

        var exists = await repository.ExistsWithSkuAsync(Sku.From("NOT-THERE"));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsWithSkuAsync_MatchesRegardlessOfTheCasingSuppliedByTheCaller()
    {
        await SeedAsync(AProduct(sku: "DSK-STAND-160"));

        var (context, repository) = NewRepository();
        await using var _ = context;

        var exists = await repository.ExistsWithSkuAsync(Sku.From("dsk-stand-160"));

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetBySkuAsync_ReturnsTheAggregate()
    {
        var seeded = AProduct(sku: "DSK-STAND-160");
        await SeedAsync(seeded);

        var (context, repository) = NewRepository();
        await using var _ = context;

        var found = await repository.GetBySkuAsync(Sku.From("DSK-STAND-160"));

        found.Should().NotBeNull();
        found!.Id.Should().Be(seeded.Id);
    }

    [Fact]
    public async Task GetBySkuAsync_ReturnsNullForAnUnknownSku()
    {
        var (context, repository) = NewRepository();
        await using var _ = context;

        var found = await repository.GetBySkuAsync(Sku.From("NOT-THERE"));

        found.Should().BeNull();
    }

    // --- Id lookup ---------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ReturnsTheAggregateOrNull()
    {
        var seeded = AProduct();
        await SeedAsync(seeded);

        var (context, repository) = NewRepository();
        await using var _ = context;

        (await repository.GetByIdAsync(seeded.Id)).Should().NotBeNull();
        (await repository.GetByIdAsync(Guid.CreateVersion7())).Should().BeNull();
    }

    // --- Listing -----------------------------------------------------------

    [Fact]
    public async Task ListAsync_PagesAndReportsTheUnpagedTotal()
    {
        await SeedAsync(
            AProduct(name: "Alpha", sku: "A-1"),
            AProduct(name: "Bravo", sku: "B-1"),
            AProduct(name: "Charlie", sku: "C-1"));

        var (context, repository) = NewRepository();
        await using var _ = context;

        var (items, totalCount) = await repository.ListAsync(page: 1, pageSize: 2);

        items.Should().HaveCount(2);
        totalCount.Should().Be(3);
        items.Select(p => p.Name).Should().Equal("Alpha", "Bravo");
    }

    [Fact]
    public async Task ListAsync_ReturnsTheSecondPage()
    {
        await SeedAsync(
            AProduct(name: "Alpha", sku: "A-1"),
            AProduct(name: "Bravo", sku: "B-1"),
            AProduct(name: "Charlie", sku: "C-1"));

        var (context, repository) = NewRepository();
        await using var _ = context;

        var (items, totalCount) = await repository.ListAsync(page: 2, pageSize: 2);

        items.Select(p => p.Name).Should().Equal("Charlie");
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task ListAsync_FiltersBySearchTermUsingSqlNotClientEvaluation()
    {
        await SeedAsync(
            AProduct(name: "Standing Desk", sku: "A-1"),
            AProduct(name: "Desk Lamp", sku: "B-1"),
            AProduct(name: "Aeron Chair", sku: "C-1"));

        var (context, repository) = NewRepository();
        await using var _ = context;

        var (items, totalCount) = await repository.ListAsync(1, 20, searchTerm: "desk");

        totalCount.Should().Be(2);
        items.Select(p => p.Name).Should().BeEquivalentTo("Standing Desk", "Desk Lamp");
    }

    [Fact]
    public async Task ListAsync_ExcludesInactiveProductsByDefault()
    {
        var inactive = AProduct(name: "Retired", sku: "R-1");
        inactive.Deactivate();

        await SeedAsync(AProduct(name: "Active", sku: "A-1"), inactive);

        var (context, repository) = NewRepository();
        await using var _ = context;

        var (activeOnly, activeTotal) = await repository.ListAsync(1, 20);
        var (all, allTotal) = await repository.ListAsync(1, 20, activeOnly: false);

        activeTotal.Should().Be(1);
        activeOnly.Select(p => p.Name).Should().Equal("Active");
        allTotal.Should().Be(2);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_ProjectsValueObjectsOnListedResults()
    {
        await SeedAsync(AProduct(sku: "DSK-STAND-160"));

        var (context, repository) = NewRepository();
        await using var _ = context;

        var (items, _) = await repository.ListAsync(1, 20);

        items.Single().Sku.Value.Should().Be("DSK-STAND-160");
        items.Single().Price.Should().Be(Money.From(749.99m, "USD"));
    }

    // --- Writes ------------------------------------------------------------

    [Fact]
    public async Task AddAndRemove_PersistThroughTheUnitOfWork()
    {
        var product = AProduct();

        var (writeContext, writeRepository) = NewRepository();
        await using (writeContext)
        {
            writeRepository.Add(product);
            await writeContext.SaveChangesAsync();
        }

        var (deleteContext, deleteRepository) = NewRepository();
        await using (deleteContext)
        {
            var loaded = await deleteRepository.GetByIdAsync(product.Id);
            deleteRepository.Remove(loaded!);
            await deleteContext.SaveChangesAsync();
        }

        var (readContext, readRepository) = NewRepository();
        await using var _ = readContext;

        (await readRepository.GetByIdAsync(product.Id)).Should().BeNull();
    }
}
