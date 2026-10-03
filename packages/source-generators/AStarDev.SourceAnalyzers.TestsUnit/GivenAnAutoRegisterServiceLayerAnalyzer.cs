using System.Collections.Immutable;
using AStarDev.SourceAnalyzers.TestsUnit.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AStarDev.SourceAnalyzers.TestsUnit;

public class GivenAnAutoRegisterServiceLayerAnalyzer
{
    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var compilation = CompilationHelpers.CreateCompilation(source);
        var analyzer = new AutoRegisterServiceLayerAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics;
    }

    [Fact]
    public async Task when_auto_register_service_has_no_arguments_then_reports_ASTARSVC001()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[AutoRegisterService]
public class Foo;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == AutoRegisterServiceLayerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_auto_register_service_specifies_only_lifetime_then_reports_ASTARSVC001()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[AutoRegisterService(ServiceLifetime.Singleton)]
public class Foo;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == AutoRegisterServiceLayerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_auto_register_service_specifies_only_as_self_then_reports_ASTARSVC001()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[AutoRegisterService(AsSelf = true)]
public class Foo;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == AutoRegisterServiceLayerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_auto_register_service_specifies_layer_positionally_then_reports_no_diagnostic()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[AutoRegisterService(ServiceLifetime.Singleton, Layer.Domain)]
public class Foo;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AutoRegisterServiceLayerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_auto_register_service_specifies_layer_by_name_then_reports_no_diagnostic()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[AutoRegisterService(layer: Layer.Application)]
public class Foo;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AutoRegisterServiceLayerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_auto_register_service_explicitly_specifies_miscellaneous_layer_then_reports_no_diagnostic()
    {
        const string source = @"using AStarDev.SourceGeneratorAttributes;
namespace TestNamespace;
[AutoRegisterService(layer: Layer.Miscellaneous)]
public class Foo;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AutoRegisterServiceLayerAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task when_class_has_no_auto_register_service_attribute_then_reports_no_diagnostic()
    {
        const string source = @"namespace TestNamespace;
public class Foo;";

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == AutoRegisterServiceLayerAnalyzer.DiagnosticId);
    }
}
