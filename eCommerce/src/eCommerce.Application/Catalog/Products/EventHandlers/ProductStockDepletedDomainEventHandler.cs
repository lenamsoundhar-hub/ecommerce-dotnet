using eCommerce.Application.Common.Events;
using eCommerce.Domain.Catalog.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace eCommerce.Application.Catalog.Products.EventHandlers;

/// <summary>
/// Reacts to a product selling out. Logging is a placeholder for the real
/// downstream work — notifying merchandising, triggering a reorder, and so on.
/// </summary>
public sealed class ProductStockDepletedDomainEventHandler
    : INotificationHandler<DomainEventNotification<ProductStockDepletedDomainEvent>>
{
    private readonly ILogger<ProductStockDepletedDomainEventHandler> _logger;

    public ProductStockDepletedDomainEventHandler(
        ILogger<ProductStockDepletedDomainEventHandler> logger) => _logger = logger;

    public Task Handle(
        DomainEventNotification<ProductStockDepletedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "Product {ProductId} ({Sku}) is out of stock",
            domainEvent.ProductId,
            domainEvent.Sku.Value);

        return Task.CompletedTask;
    }
}
