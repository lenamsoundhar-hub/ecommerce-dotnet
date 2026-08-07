using eCommerce.Domain.Catalog;
using eCommerce.Domain.Catalog.Events;
using eCommerce.Domain.Exceptions;
using eCommerce.Domain.ValueObjects;

namespace eCommerce.Domain.UnitTests.Catalog;

public class ProductTests
{
    private static Product ACatalogueProduct(int stockQuantity = 10) => Product.Create(
        "Standing Desk",
        "Electric height-adjustable desk.",
        Sku.From("DSK-STAND-160"),
        Money.From(749.99m, "USD"),
        stockQuantity);

    [Fact]
    public void Create_StartsActiveAndRecordsTheCreatedEvent()
    {
        var product = ACatalogueProduct();

        product.Id.Should().NotBe(Guid.Empty);
        product.IsActive.Should().BeTrue();
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedDomainEvent>()
            .Which.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public void Create_TrimsTheNameAndDescription()
    {
        var product = Product.Create(
            "  Padded Name  ",
            "  Padded description.  ",
            Sku.From("PAD-1"),
            Money.From(1m, "USD"),
            1);

        product.Name.Should().Be("Padded Name");
        product.Description.Should().Be("Padded description.");
    }

    [Fact]
    public void Create_RejectsABlankName()
    {
        var act = () => Product.Create("  ", "d", Sku.From("A-1"), Money.From(1m, "USD"), 1);

        act.Should().Throw<DomainException>().WithMessage("*name is required*");
    }

    [Fact]
    public void Create_RejectsNegativeStock()
    {
        var act = () => Product.Create("n", "d", Sku.From("A-1"), Money.From(1m, "USD"), -1);

        act.Should().Throw<DomainException>().WithMessage("*Stock quantity*negative*");
    }

    [Fact]
    public void ChangePrice_RecordsTheOldAndNewPrice()
    {
        var product = ACatalogueProduct();
        product.ClearDomainEvents();

        product.ChangePrice(Money.From(699.00m, "USD"));

        product.Price.Amount.Should().Be(699.00m);
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductPriceChangedDomainEvent>()
            .Which.OldPrice.Amount.Should().Be(749.99m);
    }

    [Fact]
    public void ChangePrice_IsANoOpWhenThePriceIsUnchanged()
    {
        var product = ACatalogueProduct();
        product.ClearDomainEvents();

        product.ChangePrice(Money.From(749.99m, "USD"));

        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangePrice_RefusesToSwitchCurrency()
    {
        var product = ACatalogueProduct();

        var act = () => product.ChangePrice(Money.From(700m, "EUR"));

        act.Should().Throw<DomainException>().WithMessage("*USD*EUR*");
    }

    [Fact]
    public void RemoveStock_DecrementsTheQuantity()
    {
        var product = ACatalogueProduct(stockQuantity: 10);

        product.RemoveStock(3);

        product.StockQuantity.Should().Be(7);
        product.IsInStock.Should().BeTrue();
    }

    [Fact]
    public void RemoveStock_RefusesToOversell()
    {
        var product = ACatalogueProduct(stockQuantity: 2);

        var act = () => product.RemoveStock(3);

        act.Should().Throw<DomainException>().WithMessage("*only 2 in stock*");
        product.StockQuantity.Should().Be(2);
    }

    [Fact]
    public void RemoveStock_RecordsDepletionWhenTheLastUnitGoes()
    {
        var product = ACatalogueProduct(stockQuantity: 2);
        product.ClearDomainEvents();

        product.RemoveStock(2);

        product.IsInStock.Should().BeFalse();
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductStockDepletedDomainEvent>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RemoveStock_RejectsNonPositiveQuantities(int quantity)
    {
        var act = () => ACatalogueProduct().RemoveStock(quantity);

        act.Should().Throw<DomainException>().WithMessage("*must be positive*");
    }

    [Fact]
    public void Restock_IncreasesTheQuantity()
    {
        var product = ACatalogueProduct(stockQuantity: 1);

        product.Restock(5);

        product.StockQuantity.Should().Be(6);
    }

    [Fact]
    public void Deactivate_RecordsTheEventOnlyOnTheFirstCall()
    {
        var product = ACatalogueProduct();
        product.ClearDomainEvents();

        product.Deactivate();
        product.Deactivate();

        product.IsActive.Should().BeFalse();
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductDeactivatedDomainEvent>();
    }

    [Fact]
    public void ADeactivatedProductRejectsModification()
    {
        var product = ACatalogueProduct();
        product.Deactivate();

        var changePrice = () => product.ChangePrice(Money.From(1m, "USD"));
        var restock = () => product.Restock(1);

        changePrice.Should().Throw<DomainException>().WithMessage("*deactivated*");
        restock.Should().Throw<DomainException>().WithMessage("*deactivated*");
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheRecordedEvents()
    {
        var product = ACatalogueProduct();

        product.ClearDomainEvents();

        product.DomainEvents.Should().BeEmpty();
    }
}
