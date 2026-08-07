namespace eCommerce.Domain.Common;

/// <summary>
/// A fact that has already happened inside the domain. The Application layer
/// adapts these onto its own dispatch mechanism so the Domain stays
/// dependency-free.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>Convenience base that stamps the identity and timestamp.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
