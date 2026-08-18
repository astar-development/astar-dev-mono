using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DocumentSpan = Avalonia.Controls.Documents.Span;

namespace AStar.Dev.Wallpaper.Scraper.Home;

/// <summary>
///     Parses a small subset of Avalonia's inline text markup (<c>Run</c>, <c>Span</c>, <c>Bold</c>, <c>Italic</c>,
///     with <c>FontSize</c>/<c>Foreground</c>/<c>FontWeight</c>/<c>FontStyle</c> attributes) out of a plain runtime
///     string. Avalonia only parses inline markup written literally in .axaml at compile time, so a string produced
///     at runtime (for example via an <see cref="IProgress{T}" /> callback) needs this to render formatted rather
///     than showing the raw tag text.
/// </summary>
public static partial class StatusMarkupParser
{
    [GeneratedRegex(@"<(?<slash>/?)(?<name>Run|Span|Bold|Italic)(?<attrs>(?:\s+[A-Za-z]+\s*=\s*""[^""]*"")*)\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"(?<key>[A-Za-z]+)\s*=\s*""(?<value>[^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex AttributeRegex();

    [GeneratedRegex("</Run\\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex RunClosingTagRegex();

    /// <summary>
    ///     Parses <paramref name="text" /> into a list of <see cref="Inline" /> instances, applying any recognised
    ///     markup. Falls back to a single plain <see cref="Run" /> containing the original text if the markup is
    ///     malformed, so scraped content containing incidental '&lt;' or '&amp;' characters never throws.
    /// </summary>
    public static IReadOnlyList<Inline> Parse(string text)
    {
        try
        {
            return ParseCore(text);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
        {
            return [new Run(text)];
        }
    }

    private static List<Inline> ParseCore(string text)
    {
        var root = new List<Inline>();
        var stack = new Stack<(string Name, IList<Inline> Target)>();
        stack.Push((string.Empty, root));

        int position = 0;

        while (position < text.Length)
        {
            var match = TagRegex().Match(text, position);

            if (!match.Success)
            {
                AppendLiteral(stack.Peek().Target, text[position..]);

                break;
            }

            AppendLiteral(stack.Peek().Target, text[position..match.Index]);

            string name = match.Groups["name"].Value;
            bool isClosing = match.Groups["slash"].Value == "/";

            if (isClosing)
            {
                if (stack.Count <= 1 || !string.Equals(stack.Peek().Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException($"Unexpected closing tag '{name}'.");
                }

                stack.Pop();
                position = match.Index + match.Length;

                continue;
            }

            var attributes = ParseAttributes(match.Groups["attrs"].Value);

            if (string.Equals(name, "Run", StringComparison.OrdinalIgnoreCase))
            {
                position = AppendRun(text, match.Index + match.Length, attributes, stack.Peek().Target);

                continue;
            }

            var container = CreateContainer(name, attributes);
            stack.Peek().Target.Add(container);
            stack.Push((name, container.Inlines));
            position = match.Index + match.Length;
        }

        if (stack.Count != 1)
        {
            throw new FormatException("Unterminated tag.");
        }

        return root;
    }

    private static void AppendLiteral(IList<Inline> target, string literal)
    {
        if (literal.Length > 0)
        {
            target.Add(new Run(literal));
        }
    }

    private static int AppendRun(string text, int contentStart, IReadOnlyDictionary<string, string> attributes, IList<Inline> target)
    {
        var closeMatch = RunClosingTagRegex().Match(text, contentStart);

        if (!closeMatch.Success)
        {
            throw new FormatException("Unterminated <Run> tag.");
        }

        var run = new Run(text[contentStart..closeMatch.Index]);
        ApplyAttributes(run, attributes);
        target.Add(run);

        return closeMatch.Index + closeMatch.Length;
    }

    private static DocumentSpan CreateContainer(string name, IReadOnlyDictionary<string, string> attributes)
    {
        var container = name.ToUpperInvariant() switch
        {
            "BOLD" => new Bold(),
            "ITALIC" => new Italic(),
            _ => new DocumentSpan(),
        };

        ApplyAttributes(container, attributes);

        return container;
    }

    private static Dictionary<string, string> ParseAttributes(string attributeText) =>
        AttributeRegex().Matches(attributeText).Select(match => match.Groups).ToDictionary(groups => groups["key"].Value, groups => groups["value"].Value, StringComparer.OrdinalIgnoreCase);

    private static void ApplyAttributes(Inline inline, IReadOnlyDictionary<string, string> attributes)
    {
        if (attributes.TryGetValue("FontSize", out string? fontSize))
        {
            inline.FontSize = double.Parse(fontSize, CultureInfo.InvariantCulture);
        }

        if (attributes.TryGetValue("Foreground", out string? foreground))
        {
            inline.Foreground = Brush.Parse(foreground);
        }

        if (attributes.TryGetValue("FontWeight", out string? fontWeight))
        {
            inline.FontWeight = Enum.Parse<FontWeight>(fontWeight, ignoreCase: true);
        }

        if (attributes.TryGetValue("FontStyle", out string? fontStyle))
        {
            inline.FontStyle = Enum.Parse<FontStyle>(fontStyle, ignoreCase: true);
        }
    }
}
