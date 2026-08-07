using eCommerce.Domain.Exceptions;
using eCommerce.Domain.ValueObjects;

namespace eCommerce.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void From_UppercasesAndTrimsTheCurrencyCode()
    {
        var money = Money.From(10m, " usd ");

        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void From_RoundsToTwoDecimalPlaces()
    {
        Money.From(10.005m, "USD").Amount.Should().Be(10.00m);
        Money.From(10.015m, "USD").Amount.Should().Be(10.02m);
    }

    [Fact]
    public void From_RejectsNegativeAmounts()
    {
        var act = () => Money.From(-0.01m, "USD");

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("US1")]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RejectsMalformedCurrencyCodes(string currency)
    {
        var act = () => Money.From(10m, currency);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Add_SumsAmountsInTheSameCurrency()
    {
        var total = Money.From(10.50m, "USD").Add(Money.From(4.50m, "USD"));

        total.Amount.Should().Be(15.00m);
        total.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_RefusesToMixCurrencies()
    {
        var act = () => Money.From(10m, "USD").Add(Money.From(10m, "EUR"));

        act.Should().Throw<DomainException>().WithMessage("*USD*EUR*");
    }

    [Fact]
    public void Subtract_RefusesToProduceANegativeResult()
    {
        var act = () => Money.From(5m, "USD").Subtract(Money.From(10m, "USD"));

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Multiply_ScalesTheAmount()
    {
        Money.From(19.99m, "USD").Multiply(3).Amount.Should().Be(59.97m);
    }

    [Fact]
    public void Equality_ComparesByValueNotReference()
    {
        Money.From(10m, "USD").Should().Be(Money.From(10m, "usd"));
        Money.From(10m, "USD").Should().NotBe(Money.From(10m, "EUR"));
        (Money.From(10m, "USD") == Money.From(10m, "USD")).Should().BeTrue();
    }

    [Fact]
    public void Equality_ProducesMatchingHashCodesForEqualValues()
    {
        Money.From(10m, "USD").GetHashCode()
            .Should().Be(Money.From(10m, "USD").GetHashCode());
    }
}
