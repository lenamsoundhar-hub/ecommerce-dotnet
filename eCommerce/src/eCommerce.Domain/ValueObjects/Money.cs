using eCommerce.Domain.Common;
using eCommerce.Domain.Exceptions;

namespace eCommerce.Domain.ValueObjects;

/// <summary>
/// An amount paired with the currency it is denominated in. Arithmetic across
/// differing currencies is rejected rather than silently coerced.
/// </summary>
public sealed class Money : ValueObject
{
    public const int CurrencyCodeLength = 3;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Required by EF Core's materialisation; not for application use.</summary>
    private Money() => Currency = null!;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public static Money From(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException($"Amount cannot be negative, but was {amount}.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        var normalised = currency.Trim().ToUpperInvariant();

        if (normalised.Length != CurrencyCodeLength || !normalised.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException(
                $"Currency must be a {CurrencyCodeLength}-letter ISO 4217 code, but was '{currency}'.");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), normalised);
    }

    public static Money Zero(string currency) => From(0m, currency);

    public Money Add(Money other) => From(Amount + Ensure(other).Amount, Currency);

    public Money Subtract(Money other) => From(Amount - Ensure(other).Amount, Currency);

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException($"Quantity cannot be negative, but was {quantity}.");
        }

        return From(Amount * quantity, Currency);
    }

    private Money Ensure(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Currency != Currency)
        {
            throw new DomainException(
                $"Cannot combine {Currency} with {other.Currency}; convert to a single currency first.");
        }

        return other;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
