using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenImplicitConversions
{
    [Fact]
    public void when_assigning_value_then_creates_ok()
    {
        Result<int, string> result = 42;

        result.ShouldBeOfType<Ok<int, string>>();
        result.ShouldBe(new Ok<int, string>(42));
    }

    [Fact]
    public void when_assigning_error_then_creates_fail()
    {
        Result<int, string> result = "bad";

        result.ShouldBeOfType<Fail<int, string>>();
        result.ShouldBe(new Fail<int, string>("bad"));
    }
}
