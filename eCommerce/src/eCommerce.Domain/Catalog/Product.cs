using eCommerce.Domain.Catalog.Events;
using eCommerce.Domain.Common;
using eCommerce.Domain.Exceptions;
using eCommerce.Domain.ValueObjects;

namespace eCommerce.Domain.Catalog;

/// <summary>
/// A sellable item in the catalogue. State changes go through the methods below
/// so the invariants stay in one place rather than spread across callers.
/// </summary>
public sealed class Product : AggregateRoot<Guid>, IAuditableEntity
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private Product(Guid id, string name, string description, Sku sku, Money price, int stockQuantity)
        : base(id)
    {
        Name = name;
        Description = description;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        IsActive = true;
    }

    /// <summary>Required by EF Core's materialisation; not for application use.</summary>
    private Product()
    {
        Name = null!;
        Description = null!;
        Sku = null!;
        Price = null!;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public Sku Sku { get; private set; }

    public Money Price { get; private set; }

    public int StockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; set; }

    public DateTimeOffset? ModifiedOnUtc { get; set; }

    public bool IsInStock => StockQuantity > 0;

    public static Product Create(
        string name,
        string description,
        Sku sku,
        Money price,
        int stockQuantity)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(price);

        var product = new Product(
            Guid.CreateVersion7(),
            ValidateName(name),
            ValidateDescription(description),
            sku,
            price,
            ValidateStock(stockQuantity));

        product.Raise(new ProductCreatedDomainEvent(product.Id, product.Sku));

        return product;
    }

    public void ChangePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        EnsureActive();

        if (newPrice.Currency != Price.Currency)
        {
            throw new DomainException(
                $"Product {Sku} is priced in {Price.Currency} and cannot be repriced in {newPrice.Currency}.");
        }

        if (newPrice == Price)
        {
            return;
        }

        var oldPrice = Price;
        Price = newPrice;

        Raise(new ProductPriceChangedDomainEvent(Id, oldPrice, newPrice));
    }

    public void UpdateDetails(string name, string description)
    {
        EnsureActive();

        Name = ValidateName(name);
        Description = ValidateDescription(description);
    }

    public void Restock(int quantity)
    {
        EnsureActive();

        if (quantity <= 0)
        {
            throw new DomainException($"Restock quantity must be positive, but was {quantity}.");
        }

        StockQuantity += quantity;
    }

    /// <summary>
    /// Removes <paramref name="quantity"/> units from stock, e.g. when an order
    /// is placed. Refuses to oversell.
    /// </summary>
    public void RemoveStock(int quantity)
    {
        EnsureActive();

        if (quantity <= 0)
        {
            throw new DomainException($"Quantity must be positive, but was {quantity}.");
        }

        if (quantity > StockQuantity)
        {
            throw new DomainException(
                $"Cannot remove {quantity} units of {Sku}; only {StockQuantity} in stock.");
        }

        StockQuantity -= quantity;

        if (StockQuantity == 0)
        {
            Raise(new ProductStockDepletedDomainEvent(Id, Sku));
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;

        Raise(new ProductDeactivatedDomainEvent(Id));
    }

    public void Reactivate() => IsActive = true;

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException($"Product {Sku} is deactivated and cannot be modified.");
        }
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required.");
        }

        var trimmed = name.Trim();

        return trimmed.Length <= NameMaxLength
            ? trimmed
            : throw new DomainException(
                $"Product name cannot exceed {NameMaxLength} characters, but was {trimmed.Length}.");
    }

    private static string ValidateDescription(string description)
    {
        var trimmed = (description ?? string.Empty).Trim();

        return trimmed.Length <= DescriptionMaxLength
            ? trimmed
            : throw new DomainException(
                $"Product description cannot exceed {DescriptionMaxLength} characters, but was {trimmed.Length}.");
    }

    private static int ValidateStock(int stockQuantity) =>
        stockQuantity >= 0
            ? stockQuantity
            : throw new DomainException($"Stock quantity cannot be negative, but was {stockQuantity}.");
}
