using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AStarDev.SourceAnalyzers;

/// <summary>
/// Analyzer that enforces [StrongType] record structs are declared readonly and partial, matching
/// StrongTypeGenerator's syntax predicate. Without both modifiers, the generator silently skips the
/// type and no members are generated.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StrongTypePartialAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor rule = new(
        DiagnosticId,
        "StrongType record struct must be readonly and partial",
        "Record '{0}' decorated with [StrongType] must be declared partial, otherwise StrongTypeGenerator silently skips it and no members are generated",
        "AStarDev.SourceAnalyzers",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>The diagnostic ID for a [StrongType] record struct missing partial.</summary>
    public const string DiagnosticId = "ASTARID001";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not RecordDeclarationSyntax recordDecl) return;

        var symbol = context.SemanticModel.GetDeclaredSymbol(recordDecl, context.CancellationToken);
        if (symbol == null) return;

        if (!Enumerable.Any(symbol.GetAttributes(),
                attr => attr.AttributeClass?.ToDisplayString() ==
                        "AStarDev.SourceGeneratorAttributes.StrongTypeAttribute"))
        {
            return;
        }

        bool isPartial = recordDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        if (isPartial) return;

        var diag = Diagnostic.Create(rule, recordDecl.Identifier.GetLocation(), symbol.Name);
        context.ReportDiagnostic(diag);
    }
}
