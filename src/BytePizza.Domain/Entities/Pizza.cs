#nullable enable

using System.Collections.ObjectModel;
using BytePizza.Domain.Common;
using BytePizza.Domain.Enums;
using BytePizza.Domain.ValueObjects;

namespace BytePizza.Domain.Entities;

public sealed class Pizza : Entity<Guid>, IAggregateRoot
{
    private readonly ReadOnlyCollection<PizzaFlavor> _flavors;

    private Pizza(PizzaSize size, PizzaFlavor[] flavors, PizzaCrust crust)
        : base(Guid.NewGuid())
    {
        Size = size;
        _flavors = Array.AsReadOnly(flavors);
        Crust = crust;
    }

    public PizzaSize Size { get; }

    public IReadOnlyList<PizzaFlavor> Flavors => _flavors;

    public PizzaCrust Crust { get; }

    public static Result<Pizza> Create(
        PizzaSize size,
        IEnumerable<PizzaFlavor>? flavors,
        PizzaCrust? crust)
    {
        if (!Enum.IsDefined(typeof(PizzaSize), size))
        {
            return Result.Failure<Pizza>("The pizza size is invalid.");
        }

        if (flavors is null)
        {
            return Result.Failure<Pizza>("Pizza flavors are required.");
        }

        if (crust is null)
        {
            return Result.Failure<Pizza>("A pizza crust is required.");
        }

        var flavorSnapshot = flavors.ToArray();

        if (flavorSnapshot.Length == 0)
        {
            return Result.Failure<Pizza>("A pizza must have at least one flavor.");
        }

        if (flavorSnapshot.Any(flavor =>
                flavor is null
                || string.IsNullOrWhiteSpace(flavor.Name)
                || flavor.Price is null
                || flavor.Fraction is null))
        {
            return Result.Failure<Pizza>("Pizza flavors must have a name, price, and fraction.");
        }

        if (string.IsNullOrWhiteSpace(crust.Name) || crust.AdditionalPrice is null)
        {
            return Result.Failure<Pizza>("The pizza crust must have a name and a valid price.");
        }

        var maximumFlavorCount = GetMaximumFlavorCount(size);
        if (flavorSnapshot.Length > maximumFlavorCount)
        {
            return Result.Failure<Pizza>(
                $"A {size} pizza cannot have more than {maximumFlavorCount} flavors.");
        }

        var fractionsResult = FlavorFraction.ValidateTotal(flavorSnapshot.Select(flavor => flavor.Fraction));
        if (fractionsResult.IsFailure)
        {
            return Result.Failure<Pizza>(fractionsResult.Error);
        }

        var currency = flavorSnapshot[0].Price.Currency;
        if (flavorSnapshot.Any(flavor => flavor.Price.Currency != currency)
            || crust.AdditionalPrice.Currency != currency)
        {
            return Result.Failure<Pizza>("Pizza flavor and crust prices must use the same currency.");
        }

        return Result.Success(new Pizza(size, flavorSnapshot, crust));
    }

    public Result<Money> CalculatePrice()
    {
        var currency = Flavors[0].Price.Currency;

        try
        {
            var total = Crust.AdditionalPrice.Amount;
            foreach (var flavor in Flavors)
            {
                total += flavor.Price.Amount * flavor.Fraction.Proportion;
            }

            return Money.Create(total, currency);
        }
        catch (OverflowException)
        {
            return Result.Failure<Money>("The pizza price exceeds the supported monetary range.");
        }
    }

    private static int GetMaximumFlavorCount(PizzaSize size) =>
        size switch
        {
            PizzaSize.Small => 2,
            PizzaSize.Medium => 3,
            PizzaSize.Large => 4,
            _ => 0
        };
}
