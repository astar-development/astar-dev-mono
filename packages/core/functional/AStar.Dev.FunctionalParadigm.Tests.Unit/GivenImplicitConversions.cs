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

    [Fact]
    public void when_assigning_exceptional_value_then_creates_success()
    {
        Exceptional<int> exceptional = 42;

        exceptional.ShouldBeOfType<Success<int>>();
        exceptional.ShouldBe(new Success<int>(42));
    }

    [Fact]
    public void when_assigning_exceptional_exception_then_creates_failure()
    {
        var exception = new InvalidOperationException("bad");

        Exceptional<int> exceptional = exception;

        exceptional.ShouldBeOfType<Failure<int>>();
        exceptional.ShouldBe(new Failure<int>(exception));
    }
}
