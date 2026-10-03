using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStarDev.SourceGenerators.TestsUnit.Utilities;

internal static class GeneratorRunHelpers
{
    private static readonly string[] _documentationDiagnosticIds = ["CS1570", "CS1572", "CS1573", "CS1574", "CS1584", "CS1587"];

    public static GeneratorDriverRunResult Run(IIncrementalGenerator generator, CSharpCompilation compilation)
    {
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation, TestContext.Current.CancellationToken);

        return driver.GetRunResult();
    }

    public static ImmutableArray<GeneratedSourceResult> GeneratedSources(GeneratorDriverRunResult result)
        => [.. result.Results.SelectMany(generatorResult => generatorResult.GeneratedSources)];

    public static ImmutableArray<Diagnostic> DocumentationDiagnostics(CSharpCompilation compilation, string generatedText)
    {
        var tree = CSharpSyntaxTree.ParseText(generatedText, new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose));

        return [.. compilation.AddSyntaxTrees(tree).GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Location.SourceTree == tree && _documentationDiagnosticIds.Contains(diagnostic.Id))];
    }

    public static ImmutableArray<Diagnostic> CompilationErrorsInGeneratedCode(CSharpCompilation compilation, GeneratorDriverRunResult result)
    {
        var generatedTrees = result.GeneratedTrees;

        return [.. compilation.AddSyntaxTrees(generatedTrees).GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Location.SourceTree is { } tree && generatedTrees.Contains(tree))];
    }
}
