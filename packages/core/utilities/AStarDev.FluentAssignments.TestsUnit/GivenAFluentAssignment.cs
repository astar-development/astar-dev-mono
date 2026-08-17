namespace AStar.Dev.FluentAssignments.TestsUnit;

public sealed class GivenAFluentAssignment
{
    [Fact]
    public void when_all_criteria_are_matched_then_the_value_is_assigned()
    {
        var sut = new AnyClass
        {
            Id = 10.WillBeSet().IfItIs().NotNull().And().ItIsGreaterThan(5).And().ItIsLessThan(11)
        };

        sut.Id.ShouldBe(10);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void when_will_be_set_is_called_then_it_returns_the_same_value_regardless_of_sign(int value) => value.WillBeSet().ShouldBe(value);

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void when_if_it_is_is_called_then_it_returns_the_same_value_regardless_of_sign(int value) => value.IfItIs().ShouldBe(value);

    [Fact]
    public void when_not_null_is_called_on_a_null_value_then_an_argument_exception_is_thrown()
    {
        int? nullValue = null;

        Action comparison = () => nullValue.NotNull();

        _ = comparison.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void when_and_is_called_then_it_returns_the_same_value_regardless_of_sign(int value) => value.And().ShouldBe(value);

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void when_the_value_is_greater_than_the_specified_minimum_then_it_is_greater_than_returns_the_same_value(int value) => value.ItIsGreaterThan(-11).ShouldBe(value);

    [Theory]
    [InlineData(9, 10)]
    [InlineData(0, 0)]
    [InlineData(-20, -10)]
    public void when_the_value_is_not_greater_than_the_specified_minimum_then_it_is_greater_than_throws(int value, int maximum)
    {
        Action comparison = () => value.ItIsGreaterThan(maximum);

        comparison.ShouldThrow<ArgumentException>().Message.ShouldBe($"The specified value of {value} was not greater than the specified minimum of {maximum} (Parameter 'minimum')");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void when_the_value_is_less_than_the_specified_maximum_then_it_is_less_than_returns_the_same_value(int value) => value.ItIsLessThan(value + 1).ShouldBe(value);

    [Theory]
    [InlineData(10, 9)]
    [InlineData(0, 0)]
    [InlineData(-10, -10)]
    public void when_the_value_is_not_less_than_the_specified_maximum_then_it_is_less_than_throws(int value, int minimum)
    {
        Action comparison = () => value.ItIsLessThan(minimum);

        comparison.ShouldThrow<ArgumentException>().Message.ShouldBe($"The specified value of {value} was not less than the specified maximum of {minimum} (Parameter 'maximum')");
    }

    private sealed class AnyClass
    {
        public int Id { get; set; }
    }
}
