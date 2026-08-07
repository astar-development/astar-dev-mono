using System.Collections.Immutable;
using AStarDev.SourceAnalyzers.Tests.Unit.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AStarDev.SourceAnalyzers.Tests.Unit;

public class GivenAStrongIdReadonlyPartialAnalyzer
{
    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var compilation = CompilationHelpers.CreateCompilation(source);
        var analyzer = new StrongIdReadonlyPartialAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics;
    }

    [Fact]
    public async Task when_strong_id_struct_is_missing_readonly_then_reports_ASTARID001()
    {
        const string source = @"using AStarDev.SourceGenerators.Attributes;
namespace TestNamespace;
[StrongId]
public partial record struct MyId;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == StrongIdReadonlyPartialAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_strong_id_struct_is_missing_partial_then_reports_ASTARID001()
    {
        const string source = @"using AStarDev.SourceGenerators.Attributes;
namespace TestNamespace;
[StrongId]
public readonly record struct MyId;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == StrongIdReadonlyPartialAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_strong_id_struct_is_missing_both_readonly_and_partial_then_reports_ASTARID001()
    {
        const string source = @"using AStarDev.SourceGenerators.Attributes;
namespace TestNamespace;
[StrongId]
public record struct MyId;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == StrongIdReadonlyPartialAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_strong_id_struct_is_readonly_and_partial_then_reports_no_diagnostic()
    {
        const string source = @"using AStarDev.SourceGenerators.Attributes;
namespace TestNamespace;
[StrongId]
public readonly partial record struct MyId;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == StrongIdReadonlyPartialAnalyzer.DiagnosticId);
    }
}
