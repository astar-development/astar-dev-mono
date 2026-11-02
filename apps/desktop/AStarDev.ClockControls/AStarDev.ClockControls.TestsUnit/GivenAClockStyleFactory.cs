namespace AStarDev.ClockControls.TestsUnit;

public sealed class GivenAClockStyleFactory
{
    [Theory]
    [InlineData("Digital")]
    [InlineData("digital")]
    [InlineData("DIGITAL")]
    public void when_the_key_is_digital_in_any_case_then_a_digital_style_is_created(string key)
    {
        ClockStyleFactory.Create(key).ShouldBeOfType<DigitalClockStyle>();
    }

    [Theory]
    [InlineData("Analogue")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    [InlineData(null)]
    public void when_the_key_is_analogue_blank_or_unknown_then_an_analogue_style_is_created(string? key)
    {
        ClockStyleFactory.Create(key).ShouldBeOfType<AnalogueClockStyle>();
    }

    [Fact]
    public void when_a_digital_style_is_converted_to_a_key_then_the_key_round_trips()
    {
        ClockStyleFactory.Create(ClockStyleFactory.Digital().ToKey()).ShouldBe(ClockStyleFactory.Digital());
    }

    [Fact]
    public void when_an_analogue_style_is_converted_to_a_key_then_the_key_round_trips()
    {
        ClockStyleFactory.Create(ClockStyleFactory.Analogue().ToKey()).ShouldBe(ClockStyleFactory.Analogue());
    }
}
