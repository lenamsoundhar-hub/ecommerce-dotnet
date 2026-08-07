using eCommerce.Domain.Common;
using MediatR;

namespace eCommerce.Application.Common.Events;

/// <summary>
/// Adapts a Domain event onto MediatR's notification pipeline. This wrapper is
/// what keeps the Domain project free of a MediatR reference — handlers are
/// written against <c>DomainEventNotification&lt;SomeDomainEvent&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
