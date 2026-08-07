using eCommerce.Domain.Common;

namespace eCommerce.Application.Common.Events;

/// <summary>
/// Publishes domain events after the unit of work has been committed, so
/// handlers never observe state that was later rolled back.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
