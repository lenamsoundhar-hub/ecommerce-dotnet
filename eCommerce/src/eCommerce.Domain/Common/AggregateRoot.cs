namespace eCommerce.Domain.Common;

/// <summary>
/// Consistency boundary of the domain. Only aggregate roots are loaded and
/// saved by repositories, and only they record domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    protected AggregateRoot() { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called after the events have been dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
