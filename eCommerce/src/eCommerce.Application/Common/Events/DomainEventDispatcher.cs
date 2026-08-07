using System.Collections.Concurrent;
using eCommerce.Domain.Common;
using MediatR;

namespace eCommerce.Application.Common.Events;

/// <inheritdoc cref="IDomainEventDispatcher"/>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<IDomainEvent, INotification>> Wrappers = new();

    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher) => _publisher = publisher;

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var wrap = Wrappers.GetOrAdd(domainEvent.GetType(), CreateWrapper);

            await _publisher.Publish(wrap(domainEvent), cancellationToken);
        }
    }

    /// <summary>
    /// Builds a delegate that boxes an event into
    /// <see cref="DomainEventNotification{TDomainEvent}"/> closed over its
    /// concrete type, so MediatR dispatches to the specific handler rather than
    /// to one registered for <see cref="IDomainEvent"/>. Cached per event type
    /// to keep reflection off the hot path.
    /// </summary>
    private static Func<IDomainEvent, INotification> CreateWrapper(Type domainEventType)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEventType);

        return domainEvent => (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
    }
}
