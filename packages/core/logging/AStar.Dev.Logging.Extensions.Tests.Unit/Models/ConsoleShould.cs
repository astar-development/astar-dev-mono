using AStar.Dev.Logging.Extensions.Models;
using Console = AStar.Dev.Logging.Extensions.Models.Console;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Console))]
public class ConsoleShould
{
    [Fact]
    public void Console_DefaultValues_ShouldInitializeCorrectly()
    {
        var console = new Console();

        Assert.NotNull(console);
        Assert.NotNull(console.FormatterName);
        Assert.Equal(string.Empty, console.FormatterName);
        Assert.NotNull(console.FormatterOptions);
        Assert.IsType<FormatterOptions>(console.FormatterOptions);
    }

    [Fact]
    public void Console_SetFormatterName_ShouldUpdateValue()
    {
        var console               = new Console();
        var expectedFormatterName = "CustomFormatter";

        console.FormatterName = expectedFormatterName;

        Assert.Equal(expectedFormatterName, console.FormatterName);
    }

    [Fact]
    public void Console_SetFormatterOptions_ShouldUpdateValue()
    {
        var console = new Console();

        var customFormatterOptions = new FormatterOptions
                                     {
                                         SingleLine        = true,
                                         IncludeScopes     = false,
                                         TimestampFormat   = "yyyy-MM-dd",
                                         UseUtcTimestamp   = false,
                                         JsonWriterOptions = new() { Indented = true }
                                     };

        console.FormatterOptions = customFormatterOptions;

        Assert.Equal(customFormatterOptions, console.FormatterOptions);
        Assert.True(console.FormatterOptions.SingleLine);
        Assert.False(console.FormatterOptions.IncludeScopes);
        Assert.Equal("yyyy-MM-dd", console.FormatterOptions.TimestampFormat);
        Assert.False(console.FormatterOptions.UseUtcTimestamp);
        Assert.NotNull(console.FormatterOptions.JsonWriterOptions);
        Assert.True(console.FormatterOptions.JsonWriterOptions.Indented);
    }

    [Fact]
    public void FormatterOptions_DefaultValues_ShouldInitializeCorrectly()
    {
        var formatterOptions = new FormatterOptions();

        Assert.NotNull(formatterOptions);
        Assert.False(formatterOptions.SingleLine);
        Assert.False(formatterOptions.IncludeScopes);
        Assert.Equal("HH:mm:ss ", formatterOptions.TimestampFormat);
        Assert.True(formatterOptions.UseUtcTimestamp);
        Assert.NotNull(formatterOptions.JsonWriterOptions);
        Assert.False(formatterOptions.JsonWriterOptions.Indented);
    }

    [Fact]
    public void FormatterOptions_SetProperties_ShouldUpdateValues()
    {
        var formatterOptions = new FormatterOptions
                               {
                                   SingleLine        = true,
                                   IncludeScopes     = true,
                                   TimestampFormat   = "yyyy-MM-dd HH:mm",
                                   UseUtcTimestamp   = false,
                                   JsonWriterOptions = new() { Indented = true }
                               };

        Assert.True(formatterOptions.SingleLine);
        Assert.True(formatterOptions.IncludeScopes);
        Assert.Equal("yyyy-MM-dd HH:mm", formatterOptions.TimestampFormat);
        Assert.False(formatterOptions.UseUtcTimestamp);
        Assert.NotNull(formatterOptions.JsonWriterOptions);
        Assert.True(formatterOptions.JsonWriterOptions.Indented);
    }

    [Fact]
    public void JsonWriterOptions_DefaultValues_ShouldInitializeCorrectly()
    {
        var jsonWriterOptions = new JsonWriterOptions();

        Assert.NotNull(jsonWriterOptions);
        Assert.False(jsonWriterOptions.Indented);
    }

    [Fact]
    public void JsonWriterOptions_SetIndented_ShouldUpdateValue()
    {
        var jsonWriterOptions = new JsonWriterOptions { Indented = true };

        Assert.True(jsonWriterOptions.Indented);
    }
}