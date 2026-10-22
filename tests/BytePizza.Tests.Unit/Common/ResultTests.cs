using BytePizza.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BytePizza.Tests.Unit.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldContainValueAndNoError_WhenGenericResultCreated()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Success_ShouldHaveSuccessState_WhenNonGenericResultCreated()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void Failure_ShouldContainErrorAndNoValue_WhenGenericResultCreated()
    {
        var result = Result.Failure<int>("Invalid order.");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid order.");
    }

    [Fact]
    public void Value_ShouldThrowInvalidOperationException_WhenResultFailed()
    {
        var result = Result.Failure<int>("Invalid order.");

        var act = () => _ = result.Value;

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Invalid order.*");
    }

    [Fact]
    public void Failure_ShouldHaveFailureState_WhenNonGenericResultCreated()
    {
        var result = Result.Failure("Invalid order.");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid order.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Failure_ShouldRejectBlankError_WhenCreated(string error)
    {
        var act = () => Result.Failure(error);

        act.Should().Throw<ArgumentException>();
    }
}
