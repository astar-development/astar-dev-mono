using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(JsonWriterOptions))]
public class JsonWriterOptionsShould
{
    [Fact]
    public void DefaultIndented_ToFalse()
    {
        var options = new JsonWriterOptions();

        var isIndented = options.Indented;

        isIndented.ShouldBeFalse("The default value for Indented should be false.");
    }

    [Fact]
    public void SetIndented_ToTrue_ReturnsTrue()
    {
        var options = new JsonWriterOptions { Indented = true };

        options.Indented.ShouldBeTrue("Setting Indented to true should result in true.");
    }

    [Fact]
    public void SetIndented_ToFalse_ReturnsFalse()
    {
        var options = new JsonWriterOptions { Indented = false };

        options.Indented.ShouldBeFalse("Setting Indented to false should result in false.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetAndRetrieve_Indented(bool expectedValue)
    {
        var options = new JsonWriterOptions { Indented = expectedValue };

        options.Indented.ShouldBe(expectedValue, $"The Indented value should be {expectedValue}.");
    }
}