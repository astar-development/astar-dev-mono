using JetBrains.Annotations;

namespace AStar.Dev.Guard.Clauses;

[TestSubject(typeof(GuardAgainst))]
public class GivenAGuardClause
{
    [Fact]
    public void when_the_object_is_null_then_guard_against_null_throws()
    {
        Action action = () => GuardAgainst.Null<string>(null!);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void when_the_object_is_not_null_then_guard_against_null_does_not_throw()
    {
        Action action = () => GuardAgainst.Null(string.Empty);

        action.ShouldNotThrow();
    }

    [Fact]
    public void when_the_value_is_negative_then_guard_against_negative_throws()
    {
        Action action = () => GuardAgainst.Negative(-1);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void when_the_value_is_not_negative_then_guard_against_negative_does_not_throw()
    {
        Action action = () => GuardAgainst.Negative(0);

        action.ShouldNotThrow();
    }
}
