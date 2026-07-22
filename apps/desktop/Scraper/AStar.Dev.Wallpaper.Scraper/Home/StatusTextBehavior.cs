using Avalonia;
using Avalonia.Controls;

namespace AStar.Dev.Wallpaper.Scraper.Home;

/// <summary>
///     Attached behavior that renders a <see cref="TextBlock" />'s content from a plain runtime string containing
///     <see cref="StatusMarkupParser" /> markup, rebuilding <see cref="TextBlock.Inlines" /> whenever the string
///     changes. Use in place of binding directly to <see cref="TextBlock.Text" /> when the bound string may contain
///     inline formatting tags produced at runtime.
/// </summary>
public static class StatusTextBehavior
{
    /// <summary>Identifies the attached <c>FormattedText</c> property.</summary>
    public static readonly AttachedProperty<string?> FormattedTextProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, string?>("FormattedText", typeof(StatusTextBehavior));

    static StatusTextBehavior() =>
        FormattedTextProperty.Changed.AddClassHandler<TextBlock>((textBlock, args) => Apply(textBlock, args.NewValue as string));

    /// <summary>Sets the value of the attached <c>FormattedText</c> property.</summary>
    public static void SetFormattedText(TextBlock textBlock, string? value) => textBlock.SetValue(FormattedTextProperty, value);

    /// <summary>Gets the value of the attached <c>FormattedText</c> property.</summary>
    public static string? GetFormattedText(TextBlock textBlock) => textBlock.GetValue(FormattedTextProperty);

    private static void Apply(TextBlock textBlock, string? text)
    {
        if (textBlock.Inlines is not { } inlines)
        {
            return;
        }

        inlines.Clear();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (var inline in StatusMarkupParser.Parse(text))
        {
            inlines.Add(inline);
        }
    }
}
