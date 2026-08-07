using eCommerce.Domain.Common;
using eCommerce.Domain.Exceptions;

namespace eCommerce.Domain.ValueObjects;

/// <summary>
/// Stock-keeping unit: the merchant-facing identifier for a product. Normalised
/// to upper case so lookups and uniqueness checks are case-insensitive.
/// </summary>
public sealed class Sku : ValueObject
{
    public const int MaxLength = 32;

    private Sku(string value) => Value = value;

    /// <summary>Required by EF Core's materialisation; not for application use.</summary>
    private Sku() => Value = null!;

    public string Value { get; private set; }

    public static Sku From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("SKU is required.");
        }

        var normalised = value.Trim().ToUpperInvariant();

        if (normalised.Length > MaxLength)
        {
            throw new DomainException(
                $"SKU cannot exceed {MaxLength} characters, but was {normalised.Length}.");
        }

        if (!normalised.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new DomainException(
                $"SKU may contain only letters, digits, hyphens and underscores, but was '{value}'.");
        }

        return new Sku(normalised);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
