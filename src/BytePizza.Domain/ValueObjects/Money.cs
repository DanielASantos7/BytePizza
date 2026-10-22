#nullable enable

using BytePizza.Domain.Common;

namespace BytePizza.Domain.ValueObjects;

/// <summary>
/// Represents a non-negative monetary amount in a three-letter currency.
/// </summary>
/// <remarks>
/// Business rule: amounts are rounded to two decimal places using midpoint-away-from-zero,
/// matching the BytePizza MVP's BRL pricing policy.
/// </remarks>
public sealed record Money
{
    private const int DecimalPlaces = 2;

    /// <summary>
    /// Initializes a new immutable monetary value.
    /// </summary>
    /// <param name="amount">The rounded monetary amount.</param>
    /// <param name="currency">The normalized three-letter currency code.</param>
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Gets the monetary amount rounded to two decimal places.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the normalized three-letter currency code.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Creates a monetary value after validating and rounding its amount.
    /// </summary>
    /// <param name="amount">The amount before rounding.</param>
    /// <param name="currency">The ISO-style three-letter currency code; defaults to BRL.</param>
    /// <returns>A successful result containing the value, or a failure describing invalid input.</returns>
    public static Result<Money> Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
        {
            return Result.Failure<Money>("Money amounts cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure<Money>("A currency code is required.");
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(character => character is < 'A' or > 'Z'))
        {
            return Result.Failure<Money>("Currency must be a three-letter code.");
        }

        // Monetary values use a consistent two-decimal scale and midpoint-away-from-zero rounding.
        var roundedAmount = decimal.Round(amount, DecimalPlaces, MidpointRounding.AwayFromZero);

        return Result.Success(new Money(roundedAmount, normalizedCurrency));
    }

    /// <summary>
    /// Adds another monetary value with the same currency.
    /// </summary>
    /// <param name="other">The amount to add.</param>
    /// <returns>The rounded sum, or a failure for mismatched currencies or overflow.</returns>
    public Result<Money> Add(Money other)
    {
        if (other is null)
        {
            return Result.Failure<Money>("The amount to add is required.");
        }

        if (!HasSameCurrency(other))
        {
            return Result.Failure<Money>("Money values must use the same currency.");
        }

        try
        {
            return Create(Amount + other.Amount, Currency);
        }
        catch (OverflowException)
        {
            return Result.Failure<Money>("The monetary operation exceeds the supported range.");
        }
    }

    /// <summary>
    /// Subtracts another monetary value without allowing a negative result.
    /// </summary>
    /// <param name="other">The amount to subtract.</param>
    /// <returns>The rounded difference, or a failure for mismatched currencies, negative results, or overflow.</returns>
    public Result<Money> Subtract(Money other)
    {
        if (other is null)
        {
            return Result.Failure<Money>("The amount to subtract is required.");
        }

        if (!HasSameCurrency(other))
        {
            return Result.Failure<Money>("Money values must use the same currency.");
        }

        if (other.Amount > Amount)
        {
            return Result.Failure<Money>("Money operations cannot produce a negative amount.");
        }

        return Create(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Multiplies this amount by a non-negative factor.
    /// </summary>
    /// <param name="factor">The multiplier.</param>
    /// <returns>The rounded product, or a failure for a negative factor or overflow.</returns>
    public Result<Money> Multiply(decimal factor)
    {
        if (factor < 0)
        {
            return Result.Failure<Money>("Money amounts cannot be multiplied by a negative factor.");
        }

        try
        {
            return Create(Amount * factor, Currency);
        }
        catch (OverflowException)
        {
            return Result.Failure<Money>("The monetary operation exceeds the supported range.");
        }
    }

    /// <summary>
    /// Determines whether another amount uses the same currency.
    /// </summary>
    /// <param name="other">The monetary value to compare.</param>
    /// <returns><see langword="true"/> when both values use the same currency.</returns>
    private bool HasSameCurrency(Money other) => Currency == other.Currency;
}
