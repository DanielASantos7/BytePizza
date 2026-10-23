#nullable enable

using System.Numerics;
using BytePizza.Domain.Common;

namespace BytePizza.Domain.ValueObjects;

/// <summary>
/// Represents an exact positive fraction of a pizza assigned to one flavor.
/// </summary>
/// <remarks>
/// Business rule: each flavor must occupy a positive part of a pizza, and the fractions
/// selected for one pizza must add up to exactly one whole pizza.
/// </remarks>
public sealed record FlavorFraction
{
    /// <summary>
    /// Initializes a normalized flavor fraction.
    /// </summary>
    /// <param name="numerator">The reduced fraction numerator.</param>
    /// <param name="denominator">The reduced fraction denominator.</param>
    private FlavorFraction(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>
    /// Gets the reduced numerator of this fraction.
    /// </summary>
    public int Numerator { get; }

    /// <summary>
    /// Gets the reduced denominator of this fraction.
    /// </summary>
    public int Denominator { get; }

    /// <summary>
    /// Gets this fraction as a decimal proportion for price calculations.
    /// </summary>
    public decimal Proportion => (decimal)Numerator / Denominator;

    /// <summary>
    /// Creates a positive fraction that does not exceed one whole pizza.
    /// </summary>
    /// <param name="numerator">The fraction numerator.</param>
    /// <param name="denominator">The fraction denominator.</param>
    /// <returns>A normalized fraction, or a failure when the fraction is outside the valid range.</returns>
    public static Result<FlavorFraction> Create(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return Result.Failure<FlavorFraction>("The fraction denominator must be greater than zero.");
        }

        if (numerator <= 0 || numerator > denominator)
        {
            return Result.Failure<FlavorFraction>("A flavor fraction must be greater than zero and no greater than one.");
        }

        var greatestCommonDivisor = GreatestCommonDivisor(numerator, denominator);

        return Result.Success(new FlavorFraction(
            numerator / greatestCommonDivisor,
            denominator / greatestCommonDivisor));
    }

    /// <summary>
    /// Validates that a collection of flavor fractions represents exactly one whole pizza.
    /// </summary>
    /// <param name="fractions">The fractions assigned to the pizza flavors.</param>
    /// <returns>Success when the fractions total exactly one; otherwise, a failure result.</returns>
    public static Result ValidateTotal(IEnumerable<FlavorFraction> fractions)
    {
        if (fractions is null)
        {
            return Result.Failure("Flavor fractions are required.");
        }

        var totalNumerator = BigInteger.Zero;
        var totalDenominator = BigInteger.One;
        var hasFractions = false;

        foreach (var fraction in fractions)
        {
            if (fraction is null)
            {
                return Result.Failure("Flavor fractions cannot contain null values.");
            }

            hasFractions = true;

            // Accumulate rational values exactly so combinations such as three 1/3 portions total 1.
            totalNumerator =
                (totalNumerator * fraction.Denominator)
                + ((BigInteger)fraction.Numerator * totalDenominator);
            totalDenominator *= fraction.Denominator;
        }

        if (!hasFractions || totalNumerator != totalDenominator)
        {
            return Result.Failure("Flavor fractions must total exactly one whole pizza.");
        }

        return Result.Success();
    }

    /// <summary>
    /// Reduces a positive fraction to its simplest terms.
    /// </summary>
    /// <param name="left">The first positive integer.</param>
    /// <param name="right">The second positive integer.</param>
    /// <returns>The greatest common divisor of the two integers.</returns>
    private static int GreatestCommonDivisor(int left, int right)
    {
        while (right != 0)
        {
            (left, right) = (right, left % right);
        }

        return left;
    }
}
