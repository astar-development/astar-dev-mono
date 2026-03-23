namespace AStar.Dev.FluentAssignments.Unit.Tests;

public class FluentAssignmentsExtensionsShould
{
    [Fact]
    public void AssignTheValueWhenTheCriteriaIsMatched()
    {
        var sut = new AnyClass
        {
            Id = 10.WillBeSet().IfItIs().NotNull().And().ItIsGreaterThan(5).And().ItIsLessThan(11)
        };

        _ = sut.Id.Should().Be(10);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void ReturnTheSameValueFromWillBeSetWhetherNegativePositiveOrZero(int value) => value.WillBeSet().Should().Be(value);

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void ReturnTheSameValueFromIfItIsWhetherNegativePositiveOrZero(int value) => value.IfItIs().Should().Be(value);

    [Fact]
    public void ThrowExceptionWhenNotNullIsCalledOnNullValue()
    {
        int? nullValue = null;

        Action comparison = () => nullValue.NotNull();

        _ = comparison.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void ReturnTheSameValueFromTheAndExtensionWhetherNegativePositiveOrZero(int value) => value.And().Should().Be(value);

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void ReturnTheSameValueFromItIsGreaterThanWhenGreaterThanTheSpecifiedValue(int value) => value.ItIsGreaterThan(-11).Should().Be(value);

    [Theory]
    [InlineData(9, 10)]
    [InlineData(0, 0)]
    [InlineData(-20, -10)]
    public void ThrowExceptionWhenItIsGreaterThanIsCalledWithValueLessThanSpecifiedMaximum(int value, int maximum)
    {
        Action comparison = () => value.ItIsGreaterThan(maximum);

        _ = comparison.Should().Throw<ArgumentException>().WithMessage($"The specified value of {value} was not greater than the specified minimum of {maximum} (Parameter 'minimum')");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [InlineData(0)]
    public void ReturnTheSameValueFromItIsLessThanWhenLessThanTheSpecifiedValue(int value) => value.ItIsLessThan(value + 1).Should().Be(value);

    [Theory]
    [InlineData(10, 9)]
    [InlineData(0, 0)]
    [InlineData(-10, -10)]
    public void ThrowExceptionWhenItIsLessThanIsCalledWithValueGreaterThanOrEqualToTheSpecifiedMinimum(int value, int minimum)
    {
        Action comparison = () => value.ItIsLessThan(minimum);

        _ = comparison.Should().Throw<ArgumentException>().WithMessage($"The specified value of {value} was not less than the specified maximum of {minimum} (Parameter 'maximum')");
    }

    private class AnyClass
    {
        public int Id { get; set; }
    }
}
