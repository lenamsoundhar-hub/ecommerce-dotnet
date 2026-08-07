using eCommerce.Domain.Common;
using eCommerce.Domain.ValueObjects;

namespace eCommerce.Domain.Catalog.Events;

public sealed record ProductCreatedDomainEvent(Guid ProductId, Sku Sku) : DomainEvent;

public sealed record ProductPriceChangedDomainEvent(
    Guid ProductId,
    Money OldPrice,
    Money NewPrice) : DomainEvent;

public sealed record ProductStockDepletedDomainEvent(Guid ProductId, Sku Sku) : DomainEvent;

public sealed record ProductDeactivatedDomainEvent(Guid ProductId) : DomainEvent;
