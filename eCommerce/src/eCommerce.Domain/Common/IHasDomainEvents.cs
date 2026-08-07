namespace eCommerce.Domain.Common;

/// <summary>
/// Non-generic view of an aggregate's recorded events, so the persistence layer
/// can collect them without knowing the aggregate's identifier type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
