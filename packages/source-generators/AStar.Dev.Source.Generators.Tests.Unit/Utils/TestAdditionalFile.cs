using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AStar.Dev.Source.Generators.Tests.Unit.Utils;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515", Justification = "Test helper intended for use in generator tests")]
public sealed class TestAdditionalFile(string path, string text) : AdditionalText
{
    private readonly SourceText _text = SourceText.From(text);

    public override SourceText GetText(CancellationToken cancellationToken = new()) => _text;

    public override string Path { get; } = path;
}
