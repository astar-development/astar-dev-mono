using System.Collections.Immutable;
using AStarDev.SourceAnalyzers.TestsUnit.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AStarDev.SourceAnalyzers.TestsUnit;

public class GivenAStrongTypePartialAnalyzer
{
    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var compilation = CompilationHelpers.CreateCompilation(source);
        var analyzer = new StrongTypePartialAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics;
    }

    [Fact]
    public async Task when_strong_type_struct_is_missing_partial_then_reports_ASTARID001()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[StrongType]
public readonly record struct MyId;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == StrongTypePartialAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_strong_type_struct_is_missing_both_readonly_and_partial_then_reports_ASTARID001()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[StrongType]
public record struct MyId;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == StrongTypePartialAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_strong_type_struct_is_readonly_and_partial_then_reports_no_diagnostic()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[StrongType]
public readonly partial record struct MyId;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == StrongTypePartialAnalyzer.DiagnosticId);
    }
}
