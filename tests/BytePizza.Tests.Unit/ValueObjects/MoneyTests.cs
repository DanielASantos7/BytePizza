using BytePizza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BytePizza.Tests.Unit.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Create_ShouldUseBrlByDefault_WhenAmountIsValid()
    {
        var result = Money.Create(12.34m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(12.34m);
        result.Value.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Add_ShouldReturnSum_WhenCurrenciesMatch()
    {
        var first = Money.Create(10.25m).Value;
        var second = Money.Create(4.50m).Value;

        var result = first.Add(second);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(14.75m);
        result.Value.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Subtract_ShouldReturnDifference_WhenResultIsNonNegative()
    {
        var first = Money.Create(10.00m).Value;
        var second = Money.Create(3.25m).Value;

        var result = first.Subtract(second);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(6.75m);
    }

    [Fact]
    public void Create_ShouldRoundAwayFromZeroToTwoDecimalPlaces_WhenAmountHasExtraPrecision()
    {
        var result = Money.Create(1.235m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(1.24m);
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenAmountIsNegative()
    {
        var result = Money.Create(-0.01m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative");
    }
}
