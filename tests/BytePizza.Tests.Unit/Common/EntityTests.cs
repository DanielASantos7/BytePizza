using BytePizza.Domain.Common;
using FluentAssertions;
using Xunit;

namespace BytePizza.Tests.Unit.Common;

public sealed class EntityTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenEntitiesHaveSameTypeAndId()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.Equals(second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenEntitiesHaveDifferentIds()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperators_ShouldCompareEntityIdentity()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var sameIdentity = new TestEntity(id);
        var differentIdentity = new TestEntity(Guid.NewGuid());

        (first == sameIdentity).Should().BeTrue();
        (first != sameIdentity).Should().BeFalse();
        (first == differentIdentity).Should().BeFalse();
        (first != differentIdentity).Should().BeTrue();
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id)
    {
    }
}
