using eCommerce.Application.Common.Events;
using eCommerce.Domain.Common;

namespace eCommerce.Infrastructure.IntegrationTests;

/// <summary>Captures what the DbContext dispatched, in order.</summary>
public sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<IDomainEvent> _dispatched = [];

    public IReadOnlyList<IDomainEvent> Dispatched => _dispatched;

    /// <summary>Runs on every dispatch; use it to observe state mid-flight.</summary>
    public Action? OnDispatch { get; set; }

    public Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        _dispatched.AddRange(domainEvents);

        OnDispatch?.Invoke();

        return Task.CompletedTask;
    }
}

/// <summary>A <see cref="TimeProvider"/> whose clock only moves when told to.</summary>
public sealed class StubTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public StubTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}
