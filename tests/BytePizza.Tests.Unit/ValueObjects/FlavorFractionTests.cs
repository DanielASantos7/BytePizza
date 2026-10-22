using BytePizza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BytePizza.Tests.Unit.ValueObjects;

public sealed class FlavorFractionTests
{
    [Theory]
    [InlineData(1, 2, 1, 2)]
    [InlineData(1, 3, 1, 3)]
    [InlineData(1, 4, 1, 4)]
    public void Create_ShouldReturnReducedFraction_WhenFractionIsValid(
        int numerator,
        int denominator,
        int expectedNumerator,
        int expectedDenominator)
    {
        var result = FlavorFraction.Create(numerator, denominator);

        result.IsSuccess.Should().BeTrue();
        result.Value.Numerator.Should().Be(expectedNumerator);
        result.Value.Denominator.Should().Be(expectedDenominator);
    }

    [Fact]
    public void ValidateTotal_ShouldSucceed_WhenFractionsTotalExactlyOne()
    {
        var first = FlavorFraction.Create(1, 3).Value;
        var second = FlavorFraction.Create(1, 3).Value;
        var third = FlavorFraction.Create(1, 3).Value;

        var result = FlavorFraction.ValidateTotal([first, second, third]);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 2, 1, 4)]
    [InlineData(1, 3, 1, 3)]
    public void ValidateTotal_ShouldFail_WhenFractionsDoNotTotalOne(
        int firstNumerator,
        int firstDenominator,
        int secondNumerator,
        int secondDenominator)
    {
        var first = FlavorFraction.Create(firstNumerator, firstDenominator).Value;
        var second = FlavorFraction.Create(secondNumerator, secondDenominator).Value;

        var result = FlavorFraction.ValidateTotal([first, second]);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exactly one");
    }
}
