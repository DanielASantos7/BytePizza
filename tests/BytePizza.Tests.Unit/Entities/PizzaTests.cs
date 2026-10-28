using BytePizza.Domain.Entities;
using BytePizza.Domain.Enums;
using BytePizza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BytePizza.Tests.Unit.Entities;

public sealed class PizzaTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenFlavorFractionsTotalExactlyOne()
    {
        var flavors = new[]
        {
            CreateFlavor("Calabresa", 40m, 1, 2),
            CreateFlavor("Muzzarella", 30m, 1, 2)
        };
        var crust = CreateCrust("Catupiry", 8m);

        var result = Pizza.Create(PizzaSize.Small, flavors, crust);

        result.IsSuccess.Should().BeTrue();
        result.Value.Flavors.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorFractionsDoNotTotalOne()
    {
        var flavors = new[]
        {
            CreateFlavor("Calabresa", 40m, 1, 2),
            CreateFlavor("Muzzarella", 30m, 1, 4)
        };

        var result = Pizza.Create(PizzaSize.Small, flavors, CreateCrust("Catupiry", 8m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("total exactly one");
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorCountExceedsSizeLimit()
    {
        var flavors = new[]
        {
            CreateFlavor("Calabresa", 40m, 1, 3),
            CreateFlavor("Muzzarella", 30m, 1, 3),
            CreateFlavor("Portuguesa", 35m, 1, 3)
        };

        var result = Pizza.Create(PizzaSize.Small, flavors, CreateCrust("Catupiry", 8m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CalculatePrice_ShouldAddProportionalFlavorPricesAndCrustSurcharge_WhenPizzaIsValid()
    {
        var flavors = new[]
        {
            CreateFlavor("Calabresa", 40m, 1, 2),
            CreateFlavor("Muzzarella", 30m, 1, 2)
        };
        var pizza = Pizza.Create(PizzaSize.Small, flavors, CreateCrust("Catupiry", 8m)).Value;

        var result = pizza.CalculatePrice();

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(43m);
        result.Value.Currency.Should().Be("BRL");
    }

    private static PizzaFlavor CreateFlavor(
        string name,
        decimal price,
        int fractionNumerator,
        int fractionDenominator) =>
        new(
            name,
            Money.Create(price).Value,
            FlavorFraction.Create(fractionNumerator, fractionDenominator).Value);

    private static PizzaCrust CreateCrust(string name, decimal additionalPrice) =>
        new(name, Money.Create(additionalPrice).Value);
}
