using eCommerce.Domain.Exceptions;
using eCommerce.Domain.ValueObjects;

namespace eCommerce.Domain.UnitTests.ValueObjects;

public class SkuTests
{
    [Fact]
    public void From_NormalisesToUpperCaseAndTrims()
    {
        Sku.From("  chr-aeron-01 ").Value.Should().Be("CHR-AERON-01");
    }

    [Theory]
    [InlineData("ABC-123")]
    [InlineData("abc_123")]
    [InlineData("A1")]
    public void From_AcceptsLettersDigitsHyphensAndUnderscores(string value)
    {
        var act = () => Sku.From(value);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("HAS SPACE")]
    [InlineData("BAD!CHAR")]
    [InlineData("SLASH/ES")]
    public void From_RejectsMalformedValues(string value)
    {
        var act = () => Sku.From(value);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void From_RejectsValuesOverTheLengthLimit()
    {
        var act = () => Sku.From(new string('A', Sku.MaxLength + 1));

        act.Should().Throw<DomainException>().WithMessage($"*{Sku.MaxLength}*");
    }

    [Fact]
    public void Equality_IsCaseInsensitiveByWayOfNormalisation()
    {
        Sku.From("abc-1").Should().Be(Sku.From("ABC-1"));
    }
}
