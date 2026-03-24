using JetBrains.Annotations;

namespace AStar.Dev.Guard.Clauses;

[TestSubject(typeof(GuardAgainst))]
public class GuardAgainstShould
{
    [Fact]
    public void ThroughWhenTheObjectIsNull()
    {
        Action action = () => GuardAgainst.Null<string>(null!);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void NotThroughWhenTheObjectIsNotNull()
    {
        Action action = () => GuardAgainst.Null(string.Empty);

        action.ShouldNotThrow();
    }

    [Fact]
    public void ThroughWhenValueIsLessThanZero()
    {
        Action action = () => GuardAgainst.Negative(-1);

        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NotThroughWhenValueIsNotLessThanZero()
    {
        Action action = () => GuardAgainst.Negative(0);

        action.ShouldNotThrow();
    }
}
