using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AStarDev.SourceAnalyzers;

/// <summary>Analyzer that enforces [AutoRegisterOptions] is only applied to partial classes or structs.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutoRegisterOptionsPartialAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor rule = new(
        DiagnosticId,
        "Options class must be partial",
        "Options class '{0}' must be declared partial to support source generation",
        "AStarDev.SourceAnalyzers",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>The diagnostic ID for missing partial on options classes.</summary>
    public const string DiagnosticId = "ASTAROPT002";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDecl) return;

        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken);
        if (symbol == null) return;

        if (!Enumerable.Any(symbol.GetAttributes(),
                attr => attr.AttributeClass?.ToDisplayString() ==
                        "AStarDev.SourceGeneratorAttributes.AutoRegisterOptionsAttribute"))
        {
            return;
        }

        if (typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))) return;

        var diag = Diagnostic.Create(rule, typeDecl.Identifier.GetLocation(), symbol.Name);
        context.ReportDiagnostic(diag);
    }
}
