#nullable enable

using BytePizza.Domain.Common;

namespace BytePizza.Domain.ValueObjects;

/// <summary>
/// Represents an immutable snapshot of a customer's delivery address at order time.
/// </summary>
/// <remarks>
/// Business rule: an order keeps the validated address values used when it was placed,
/// so later customer profile edits cannot change the destination of an existing order.
/// </remarks>
public sealed record Address
{
    /// <summary>
    /// Initializes an immutable delivery address snapshot.
    /// </summary>
    /// <param name="street">The street or avenue name.</param>
    /// <param name="number">The street number or accepted delivery designation.</param>
    /// <param name="neighborhood">The neighborhood or district.</param>
    /// <param name="city">The city.</param>
    /// <param name="zipCode">The eight-digit Brazilian postal code.</param>
    /// <param name="complement">Optional additional delivery details.</param>
    private Address(
        string street,
        string number,
        string neighborhood,
        string city,
        string zipCode,
        string? complement)
    {
        Street = street;
        Number = number;
        Neighborhood = neighborhood;
        City = city;
        ZipCode = zipCode;
        Complement = complement;
    }

    /// <summary>
    /// Gets the street or avenue name.
    /// </summary>
    public string Street { get; }

    /// <summary>
    /// Gets the street number or accepted delivery designation.
    /// </summary>
    public string Number { get; }

    /// <summary>
    /// Gets the neighborhood or district.
    /// </summary>
    public string Neighborhood { get; }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the normalized postal code in the format 00000-000.
    /// </summary>
    public string ZipCode { get; }

    /// <summary>
    /// Gets optional additional delivery details.
    /// </summary>
    public string? Complement { get; }

    /// <summary>
    /// Creates a validated, normalized delivery address snapshot.
    /// </summary>
    /// <param name="street">The street or avenue name.</param>
    /// <param name="number">The street number or accepted delivery designation.</param>
    /// <param name="neighborhood">The neighborhood or district.</param>
    /// <param name="city">The city.</param>
    /// <param name="zipCode">An eight-digit Brazilian postal code, optionally containing a hyphen.</param>
    /// <param name="complement">Optional additional delivery details.</param>
    /// <returns>The immutable snapshot or a failure describing invalid address data.</returns>
    public static Result<Address> Create(
        string street,
        string number,
        string neighborhood,
        string city,
        string zipCode,
        string? complement = null)
    {
        if (string.IsNullOrWhiteSpace(street)
            || string.IsNullOrWhiteSpace(number)
            || string.IsNullOrWhiteSpace(neighborhood)
            || string.IsNullOrWhiteSpace(city))
        {
            return Result.Failure<Address>("Street, number, neighborhood, and city are required.");
        }

        if (string.IsNullOrWhiteSpace(zipCode))
        {
            return Result.Failure<Address>("A postal code is required.");
        }

        var normalizedZipCode = zipCode.Trim().Replace("-", string.Empty, StringComparison.Ordinal);

        // Brazilian delivery addresses require exactly eight digits for a valid CEP snapshot.
        if (normalizedZipCode.Length != 8 || normalizedZipCode.Any(character => character is < '0' or > '9'))
        {
            return Result.Failure<Address>("The postal code must contain exactly eight digits.");
        }

        return Result.Success(new Address(
            street.Trim(),
            number.Trim(),
            neighborhood.Trim(),
            city.Trim(),
            $"{normalizedZipCode[..5]}-{normalizedZipCode[5..]}",
            string.IsNullOrWhiteSpace(complement) ? null : complement.Trim()));
    }
}
