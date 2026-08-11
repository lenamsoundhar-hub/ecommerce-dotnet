using eCommerce.Application.Common.Events;
using eCommerce.Domain.Catalog.Events;
using eCommerce.Domain.Common;
using eCommerce.Domain.ValueObjects;
using MediatR;
using NSubstitute;

namespace eCommerce.Application.UnitTests.Common.Events;

public class DomainEventDispatcherTests
{
    private readonly List<INotification> _published = [];
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly DomainEventDispatcher _dispatcher;

    public DomainEventDispatcherTests()
    {
        _publisher
            .Publish(Arg.Do<INotification>(_published.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _dispatcher = new DomainEventDispatcher(_publisher);
    }

    private static ProductCreatedDomainEvent ACreatedEvent() =>
        new(Guid.CreateVersion7(), Sku.From("DSK-STAND-160"));

    /// <summary>
    /// The behaviour the whole class exists for. Closing the wrapper over the
    /// static type — <c>DomainEventNotification&lt;IDomainEvent&gt;</c> — would
    /// compile, publish without error, and reach no handler at all, because
    /// handlers are registered against the concrete event type.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_ClosesTheWrapperOverTheConcreteEventTypeNotTheInterface()
    {
        await _dispatcher.DispatchAsync([ACreatedEvent()]);

        _published.Should().ContainSingle()
            .Which.Should().BeOfType<DomainEventNotification<ProductCreatedDomainEvent>>();
    }

    [Fact]
    public async Task DispatchAsync_DoesNotPublishAWrapperClosedOverIDomainEvent()
    {
        await _dispatcher.DispatchAsync([ACreatedEvent()]);

        _published.Single().Should().NotBeOfType<DomainEventNotification<IDomainEvent>>();
        _published.Single().GetType().GetGenericArguments().Single()
            .Should().Be<ProductCreatedDomainEvent>();
    }

    [Fact]
    public async Task DispatchAsync_CarriesTheOriginalEventInstance()
    {
        var domainEvent = ACreatedEvent();

        await _dispatcher.DispatchAsync([domainEvent]);

        _published.Single()
            .Should().BeOfType<DomainEventNotification<ProductCreatedDomainEvent>>()
            .Which.DomainEvent.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public async Task DispatchAsync_WrapsEachEventTypeInItsOwnClosedGeneric()
    {
        var id = Guid.CreateVersion7();

        await _dispatcher.DispatchAsync([
            ACreatedEvent(),
            new ProductStockDepletedDomainEvent(id, Sku.From("KBD-MECH-65")),
            new ProductPriceChangedDomainEvent(id, Money.From(10m, "USD"), Money.From(9m, "USD")),
            new ProductDeactivatedDomainEvent(id),
        ]);

        _published.Select(n => n.GetType()).Should().Equal(
            typeof(DomainEventNotification<ProductCreatedDomainEvent>),
            typeof(DomainEventNotification<ProductStockDepletedDomainEvent>),
            typeof(DomainEventNotification<ProductPriceChangedDomainEvent>),
            typeof(DomainEventNotification<ProductDeactivatedDomainEvent>));
    }

    [Fact]
    public async Task DispatchAsync_PublishesInTheOrderTheEventsWereRecorded()
    {
        var first = ACreatedEvent();
        var second = ACreatedEvent();

        await _dispatcher.DispatchAsync([first, second]);

        _published
            .Cast<DomainEventNotification<ProductCreatedDomainEvent>>()
            .Select(n => n.DomainEvent)
            .Should().Equal(first, second);
    }

    /// <summary>
    /// The second dispatch of a type takes the cached delegate rather than
    /// rebuilding it, so this covers the <c>GetOrAdd</c> hit path.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_ReusesTheCachedWrapperForARepeatedEventType()
    {
        await _dispatcher.DispatchAsync([ACreatedEvent()]);
        await _dispatcher.DispatchAsync([ACreatedEvent()]);

        _published.Should().HaveCount(2);
        _published.Should().AllBeOfType<DomainEventNotification<ProductCreatedDomainEvent>>();
    }

    [Fact]
    public async Task DispatchAsync_PublishesNothingForAnEmptySequence()
    {
        await _dispatcher.DispatchAsync([]);

        _published.Should().BeEmpty();
        await _publisher.DidNotReceive()
            .Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_ForwardsTheCancellationToken()
    {
        using var cts = new CancellationTokenSource();

        await _dispatcher.DispatchAsync([ACreatedEvent()], cts.Token);

        await _publisher.Received(1).Publish(Arg.Any<INotification>(), cts.Token);
    }
}
