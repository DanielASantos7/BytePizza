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
        result.Error.Should().Contain("Small").And.Contain("2 flavors");
    }

    [Theory]
    [InlineData(PizzaSize.Small, 2)]
    [InlineData(PizzaSize.Medium, 3)]
    [InlineData(PizzaSize.Large, 4)]
    public void Create_ShouldSucceed_WhenFlavorCountMatchesSizeLimit(PizzaSize size, int flavorCount)
    {
        var flavors = Enumerable.Range(1, flavorCount)
            .Select(index => CreateFlavor($"Flavor {index}", 30m, 1, flavorCount))
            .ToArray();

        var result = Pizza.Create(size, flavors, CreateCrust("Tradicional", 0m));

        result.IsSuccess.Should().BeTrue();
        result.Value.Flavors.Should().HaveCount(flavorCount);
    }

    [Fact]
    public void Create_ShouldFail_WhenSizeIsUndefined()
    {
        var result = Pizza.Create(
            (PizzaSize)999,
            [CreateFlavor("Calabresa", 40m, 1, 1)],
            CreateCrust("Tradicional", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("size is invalid");
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorCollectionIsNull()
    {
        var result = Pizza.Create(PizzaSize.Small, null, CreateCrust("Tradicional", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("flavors are required");
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorCollectionIsEmpty()
    {
        var result = Pizza.Create(PizzaSize.Small, [], CreateCrust("Tradicional", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one flavor");
    }

    [Fact]
    public void Create_ShouldFail_WhenCrustIsNull()
    {
        var result = Pizza.Create(
            PizzaSize.Small,
            [CreateFlavor("Calabresa", 40m, 1, 1)],
            null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("crust is required");
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorCollectionContainsNull()
    {
        var flavors = new[] { (PizzaFlavor)null! };

        var result = Pizza.Create(PizzaSize.Small, flavors, CreateCrust("Tradicional", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name, price, and fraction");
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorNameIsBlank()
    {
        var result = Pizza.Create(
            PizzaSize.Small,
            [CreateFlavor(" ", 40m, 1, 1)],
            CreateCrust("Tradicional", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name, price, and fraction");
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorPriceIsNull()
    {
        var flavor = new PizzaFlavor("Calabresa", null!, FlavorFraction.Create(1, 1).Value);

        var result = Pizza.Create(PizzaSize.Small, [flavor], CreateCrust("Tradicional", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name, price, and fraction");
    }

    [Fact]
    public void Create_ShouldFail_WhenFlavorFractionIsNull()
    {
        var flavor = new PizzaFlavor("Calabresa", Money.Create(40m).Value, null!);

        var result = Pizza.Create(PizzaSize.Small, [flavor], CreateCrust("Tradicional", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name, price, and fraction");
    }

    [Fact]
    public void Create_ShouldFail_WhenCrustNameIsBlank()
    {
        var result = Pizza.Create(
            PizzaSize.Small,
            [CreateFlavor("Calabresa", 40m, 1, 1)],
            CreateCrust(" ", 0m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("crust must have a name");
    }

    [Fact]
    public void Create_ShouldFail_WhenCrustPriceIsNull()
    {
        var crust = new PizzaCrust("Tradicional", null!);

        var result = Pizza.Create(PizzaSize.Small, [CreateFlavor("Calabresa", 40m, 1, 1)], crust);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("crust must have a name");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_ShouldFail_WhenFlavorAndCrustCurrenciesDoNotMatch(bool crustHasDifferentCurrency)
    {
        var flavorCurrency = crustHasDifferentCurrency ? "BRL" : "USD";
        var crustCurrency = crustHasDifferentCurrency ? "USD" : "BRL";
        var flavors = new[]
        {
            CreateFlavor("Calabresa", 40m, 1, 2, flavorCurrency),
            CreateFlavor("Muzzarella", 30m, 1, 2)
        };

        var result = Pizza.Create(
            PizzaSize.Small,
            flavors,
            CreateCrust("Tradicional", 0m, crustCurrency));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("same currency");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenThreeEqualFractionsTotalExactlyOne()
    {
        var flavors = new[]
        {
            CreateFlavor("Calabresa", 40m, 1, 3),
            CreateFlavor("Muzzarella", 30m, 1, 3),
            CreateFlavor("Portuguesa", 35m, 1, 3)
        };

        var result = Pizza.Create(PizzaSize.Large, flavors, CreateCrust("Tradicional", 0m));

        result.IsSuccess.Should().BeTrue();
        result.Value.Flavors.Should().HaveCount(3);
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

    [Fact]
    public void CalculatePrice_ShouldRoundAfterSummingWeightedFlavorPrices_WhenFractionsRepeatDecimally()
    {
        var flavors = new[]
        {
            CreateFlavor("Calabresa", 0.01m, 1, 3),
            CreateFlavor("Muzzarella", 0.01m, 1, 3),
            CreateFlavor("Portuguesa", 0.01m, 1, 3)
        };
        var pizza = Pizza.Create(PizzaSize.Large, flavors, CreateCrust("Tradicional", 0m)).Value;

        var result = pizza.CalculatePrice();

        result.IsSuccess.Should().BeTrue();
        // Rounding each one-third share first would incorrectly produce zero.
        result.Value.Amount.Should().Be(0.01m);
    }

    [Fact]
    public void CalculatePrice_ShouldFail_WhenTotalExceedsSupportedMonetaryRange()
    {
        var pizza = Pizza.Create(
            PizzaSize.Small,
            [CreateFlavor("Calabresa", 1m, 1, 1)],
            new PizzaCrust("Tradicional", Money.Create(decimal.MaxValue).Value)).Value;

        var result = pizza.CalculatePrice();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceeds the supported monetary range");
    }

    private static PizzaFlavor CreateFlavor(
        string name,
        decimal price,
        int fractionNumerator,
        int fractionDenominator,
        string currency = "BRL") =>
        new(
            name,
            Money.Create(price, currency).Value,
            FlavorFraction.Create(fractionNumerator, fractionDenominator).Value);

    private static PizzaCrust CreateCrust(
        string name,
        decimal additionalPrice,
        string currency = "BRL") =>
        new(name, Money.Create(additionalPrice, currency).Value);
}
