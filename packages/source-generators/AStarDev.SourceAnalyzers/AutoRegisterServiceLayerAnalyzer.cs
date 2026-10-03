using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AStarDev.SourceAnalyzers;

/// <summary>
/// Analyzer that warns when [AutoRegisterService] does not explicitly specify a Layer. Without one, the
/// service is silently classified as Layer.Miscellaneous by ServiceRegistrationGenerator.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutoRegisterServiceLayerAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor rule = new(
        DiagnosticId,
        "AutoRegisterService should specify a Layer",
        "Class '{0}' is decorated with [AutoRegisterService] but does not specify a Layer, so it defaults to Layer.Miscellaneous. Specify Layer.Application, Layer.Domain or Layer.Infrastructure if applicable.",
        "AStarDev.SourceAnalyzers",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>The diagnostic ID for [AutoRegisterService] missing an explicit Layer.</summary>
    public const string DiagnosticId = "ASTARSVC001";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl) return;

        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl, context.CancellationToken);
        if (symbol == null) return;

        var attr = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "AStarDev.SourceGeneratorAttributes.AutoRegisterServiceAttribute");
        if (attr is null) return;

        if (attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is not AttributeSyntax attrSyntax) return;

        if (IsLayerExplicitlySupplied(attrSyntax)) return;

        var diag = Diagnostic.Create(rule, classDecl.Identifier.GetLocation(), symbol.Name);
        context.ReportDiagnostic(diag);
    }

    private static bool IsLayerExplicitlySupplied(AttributeSyntax attributeSyntax)
    {
        if (attributeSyntax.ArgumentList is null) return false;

        int positionalIndex = 0;

        foreach (var arg in attributeSyntax.ArgumentList.Arguments)
        {
            if (arg.NameEquals is not null) continue;

            if (arg.NameColon is { Name.Identifier.ValueText: "layer" }) return true;

            if (arg.NameColon is null)
            {
                if (positionalIndex == 1) return true;

                positionalIndex++;
            }
        }

        return false;
    }
}
