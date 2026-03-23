using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(FormatterOptions))]
public class FormatterOptionsShould
{
    [Fact]
    public void SingleLine_ShouldDefaultToFalse()
    {
        var options = new FormatterOptions();

        var result = options.SingleLine;

        Assert.False(result);
    }

    [Fact]
    public void SingleLine_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { SingleLine = true };

        Assert.True(options.SingleLine);
    }

    [Fact]
    public void IncludeScopes_ShouldDefaultToFalse()
    {
        var options = new FormatterOptions();

        var result = options.IncludeScopes;

        Assert.False(result);
    }

    [Fact]
    public void IncludeScopes_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { IncludeScopes = true };

        Assert.True(options.IncludeScopes);
    }

    [Fact]
    public void TimestampFormat_ShouldDefaultTo_HH_mm_ss_Space()
    {
        var options = new FormatterOptions();

        var result = options.TimestampFormat;

        Assert.Equal("HH:mm:ss ", result);
    }

    [Fact]
    public void TimestampFormat_ShouldSetAndGetCorrectly()
    {
        var options   = new FormatterOptions();
        var newFormat = "yyyy-MM-dd";

        options.TimestampFormat = newFormat;

        Assert.Equal(newFormat, options.TimestampFormat);
    }

    [Fact]
    public void UseUtcTimestamp_ShouldDefaultToTrue()
    {
        var options = new FormatterOptions();

        var result = options.UseUtcTimestamp;

        Assert.True(result);
    }

    [Fact]
    public void UseUtcTimestamp_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { UseUtcTimestamp = false };

        Assert.False(options.UseUtcTimestamp);
    }

    [Fact]
    public void JsonWriterOptions_ShouldDefaultTo_NotNull()
    {
        var options = new FormatterOptions();

        var result = options.JsonWriterOptions;

        Assert.NotNull(result);
    }

    [Fact]
    public void JsonWriterOptions_Indented_ShouldSetAndGetCorrectly()
    {
        var options = new FormatterOptions { JsonWriterOptions = { Indented = true } };

        Assert.True(options.JsonWriterOptions.Indented);
    }
}