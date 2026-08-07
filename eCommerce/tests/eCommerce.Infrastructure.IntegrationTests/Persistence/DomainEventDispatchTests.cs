using eCommerce.Domain.Catalog;
using eCommerce.Domain.Catalog.Events;
using eCommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Covers the ordering guarantee in <c>ApplicationDbContext.SaveChangesAsync</c>:
/// events are published only after the transaction commits, and are cleared
/// before dispatch so a re-entrant save cannot replay them.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class DomainEventDispatchTests : IAsyncLifetime
{
    private readonly SqlServerDatabaseFixture _fixture;

    public DomainEventDispatchTests(SqlServerDatabaseFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static Product AProduct(string sku, int stock = 5) =>
        Product.Create("Standing Desk", "A description.", Sku.From(sku), Money.From(749.99m, "USD"), stock);

    [Fact]
    public async Task SavingPublishesTheEventsTheAggregateRecorded()
    {
        var dispatcher = new RecordingDomainEventDispatcher();

        await using var context = _fixture.CreateContext(dispatcher);
        context.Products.Add(AProduct("EVT-1"));
        await context.SaveChangesAsync();

        dispatcher.Dispatched.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedDomainEvent>();
    }

    [Fact]
    public async Task EventsAreClearedFromTheAggregateBeforeDispatch()
    {
        var product = AProduct("EVT-2");
        var eventsStillOnAggregate = -1;

        var dispatcher = new RecordingDomainEventDispatcher
        {
            OnDispatch = () => eventsStillOnAggregate = product.DomainEvents.Count,
        };

        await using var context = _fixture.CreateContext(dispatcher);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        eventsStillOnAggregate.Should().Be(0);
        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task TheRowIsAlreadyCommittedByTheTimeHandlersRun()
    {
        var visibleDuringDispatch = false;
        var product = AProduct("EVT-3");

        var dispatcher = new RecordingDomainEventDispatcher();
        dispatcher.OnDispatch = () =>
        {
            // A separate context can only see the row if the save committed first.
            using var probe = _fixture.CreateContext();
            visibleDuringDispatch = probe.Products.Any(p => p.Id == product.Id);
        };

        await using var context = _fixture.CreateContext(dispatcher);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        visibleDuringDispatch.Should().BeTrue();
    }

    [Fact]
    public async Task ASecondSaveDoesNotRepublishTheSameEvents()
    {
        var dispatcher = new RecordingDomainEventDispatcher();
        var product = AProduct("EVT-4");

        await using var context = _fixture.CreateContext(dispatcher);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        product.Restock(1);
        await context.SaveChangesAsync();

        // Restock records no event, so the creation event must not reappear.
        dispatcher.Dispatched.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedDomainEvent>();
    }

    [Fact]
    public async Task NothingIsPublishedWhenTheSaveFails()
    {
        await using (var seed = _fixture.CreateContext())
        {
            seed.Products.Add(AProduct("EVT-DUP"));
            await seed.SaveChangesAsync();
        }

        var dispatcher = new RecordingDomainEventDispatcher();

        await using var context = _fixture.CreateContext(dispatcher);
        context.Products.Add(AProduct("EVT-DUP"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
        dispatcher.Dispatched.Should().BeEmpty();
    }

    [Fact]
    public async Task StockDepletionIsPublishedWhenTheLastUnitGoes()
    {
        var dispatcher = new RecordingDomainEventDispatcher();
        var product = AProduct("EVT-5", stock: 2);

        await using (var seed = _fixture.CreateContext())
        {
            seed.Products.Add(product);
            await seed.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext(dispatcher);
        var loaded = await context.Products.SingleAsync(p => p.Id == product.Id);
        loaded.RemoveStock(2);
        await context.SaveChangesAsync();

        dispatcher.Dispatched.Should().ContainSingle()
            .Which.Should().BeOfType<ProductStockDepletedDomainEvent>();
    }
}
