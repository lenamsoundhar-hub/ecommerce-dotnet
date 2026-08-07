using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Infrastructure.IntegrationTests.Persistence;

[Collection(DatabaseCollection.Name)]
public class AuditableEntityInterceptorTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SqlServerDatabaseFixture _fixture;

    public AuditableEntityInterceptorTests(SqlServerDatabaseFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static Product AProduct(string sku) =>
        Product.Create("Standing Desk", "A description.", Sku.From(sku), Money.From(749.99m, "USD"), 5);

    [Fact]
    public async Task InsertingStampsCreatedOnUtcAndLeavesModifiedNull()
    {
        var clock = new StubTimeProvider(Start);
        var product = AProduct("AUD-1");

        await using (var write = _fixture.CreateContext(timeProvider: clock))
        {
            write.Products.Add(product);
            await write.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();
        var loaded = await read.Products.SingleAsync(p => p.Id == product.Id);

        loaded.CreatedOnUtc.Should().Be(Start);
        loaded.ModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task ChangingAScalarStampsModifiedOnUtc()
    {
        var clock = new StubTimeProvider(Start);
        var product = AProduct("AUD-2");

        await using (var write = _fixture.CreateContext(timeProvider: clock))
        {
            write.Products.Add(product);
            await write.SaveChangesAsync();
        }

        clock.Advance(TimeSpan.FromHours(1));

        await using (var update = _fixture.CreateContext(timeProvider: clock))
        {
            var loaded = await update.Products.SingleAsync(p => p.Id == product.Id);
            loaded.Restock(5);
            await update.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();
        var result = await read.Products.SingleAsync(p => p.Id == product.Id);

        result.CreatedOnUtc.Should().Be(Start);
        result.ModifiedOnUtc.Should().Be(Start.AddHours(1));
    }

    /// <summary>
    /// Price lives in an owned type, so changing it leaves the owner entry
    /// Unchanged. This is the case the interceptor's owned-entity check exists
    /// for, and the one most likely to regress silently.
    /// </summary>
    [Fact]
    public async Task ChangingOnlyTheOwnedMoneyStillStampsModifiedOnUtc()
    {
        var clock = new StubTimeProvider(Start);
        var product = AProduct("AUD-3");

        await using (var write = _fixture.CreateContext(timeProvider: clock))
        {
            write.Products.Add(product);
            await write.SaveChangesAsync();
        }

        clock.Advance(TimeSpan.FromHours(2));

        await using (var update = _fixture.CreateContext(timeProvider: clock))
        {
            var loaded = await update.Products.SingleAsync(p => p.Id == product.Id);
            loaded.ChangePrice(Money.From(699.00m, "USD"));
            await update.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();
        var result = await read.Products.SingleAsync(p => p.Id == product.Id);

        result.Price.Amount.Should().Be(699.00m);
        result.ModifiedOnUtc.Should().Be(Start.AddHours(2));
    }

    [Fact]
    public async Task SavingWithNoChangesLeavesTheTimestampsAlone()
    {
        var clock = new StubTimeProvider(Start);
        var product = AProduct("AUD-4");

        await using (var write = _fixture.CreateContext(timeProvider: clock))
        {
            write.Products.Add(product);
            await write.SaveChangesAsync();
        }

        clock.Advance(TimeSpan.FromHours(3));

        await using (var noop = _fixture.CreateContext(timeProvider: clock))
        {
            await noop.Products.SingleAsync(p => p.Id == product.Id);
            await noop.SaveChangesAsync();
        }

        await using var read = _fixture.CreateContext();
        var result = await read.Products.SingleAsync(p => p.Id == product.Id);

        result.ModifiedOnUtc.Should().BeNull();
    }
}
