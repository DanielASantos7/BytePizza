using BytePizza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BytePizza.Tests.Unit.ValueObjects;

public sealed class AddressTests
{
    [Theory]
    [InlineData("12345678", "12345-678")]
    [InlineData("12345-678", "12345-678")]
    public void Create_ShouldNormalizeZipCode_WhenPostalCodeIsValid(
        string zipCode,
        string expectedZipCode)
    {
        var result = Address.Create(
            "Main Street",
            "42",
            "Downtown",
            "Springfield",
            zipCode);

        result.IsSuccess.Should().BeTrue();
        result.Value.ZipCode.Should().Be(expectedZipCode);
    }

    [Theory]
    [InlineData("", "42", "Downtown", "Springfield")]
    [InlineData("Main Street", " ", "Downtown", "Springfield")]
    [InlineData("Main Street", "42", "", "Springfield")]
    [InlineData("Main Street", "42", "Downtown", "   ")]
    public void Create_ShouldFail_WhenRequiredAddressFieldIsEmpty(
        string street,
        string number,
        string neighborhood,
        string city)
    {
        var result = Address.Create(
            street,
            number,
            neighborhood,
            city,
            "12345-678");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }
}
