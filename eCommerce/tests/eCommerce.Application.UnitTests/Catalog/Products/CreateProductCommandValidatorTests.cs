using eCommerce.Application.Catalog.Products.Commands.CreateProduct;

namespace eCommerce.Application.UnitTests.Catalog.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand AValidCommand() =>
        new("Standing Desk", "Electric desk.", "DSK-STAND-160", 749.99m, "USD", 5);

    [Fact]
    public void AWellFormedCommandPasses()
    {
        _validator.Validate(AValidCommand()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NameIsRequired(string name)
    {
        var result = _validator.Validate(AValidCommand() with { Name = name });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }

    [Theory]
    [InlineData("HAS SPACE")]
    [InlineData("BAD!CHAR")]
    [InlineData("")]
    public void SkuMustBeAlphanumericWithHyphensOrUnderscores(string sku)
    {
        var result = _validator.Validate(AValidCommand() with { Sku = sku });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Sku));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    [InlineData("US1")]
    public void CurrencyMustBeAThreeLetterCode(string currency)
    {
        var result = _validator.Validate(AValidCommand() with { Currency = currency });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Currency));
    }

    [Fact]
    public void PriceAndStockCannotBeNegative()
    {
        var result = _validator.Validate(AValidCommand() with { Price = -1m, StockQuantity = -1 });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Price));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.StockQuantity));
    }
}
