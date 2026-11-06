using System.Reflection;
using System.Text.RegularExpressions;

namespace AStarDev.LoggingExtensions.TestsUnit;

public sealed partial class GivenTheLoggerMessageTemplates
{
    public static TheoryData<string, string> Templates()
    {
        var templates = new TheoryData<string, string>();
        foreach (var method in typeof(LogMessage).Assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)))
        {
            if (method.GetCustomAttribute<LoggerMessageAttribute>() is { } attribute)
                templates.Add($"{method.DeclaringType!.Name}.{method.Name}", attribute.Message);
        }

        return templates;
    }

    [Fact]
    public void when_the_templates_are_inspected_then_some_are_found()
        => Templates().Count.ShouldBeGreaterThan(20);

    [Theory]
    [MemberData(nameof(Templates))]
    public void when_a_template_is_inspected_then_every_placeholder_is_pascal_case(string templateName, string message)
    {
        var placeholders = Placeholder().Matches(message).Select(match => match.Groups[1].Value);

        placeholders.ShouldAllBe(name => char.IsUpper(name[0]), templateName);
    }

    [GeneratedRegex(@"\{([A-Za-z_][A-Za-z0-9_]*)[^{}]*\}")]
    private static partial Regex Placeholder();
}
