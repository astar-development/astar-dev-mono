using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AStar.Dev.Source.Analyzers;

/// <summary>
/// Analyzer that enforces [AutoRegisterOptions] is only applied to partial classes or structs.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutoRegisterOptionsPartialAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic ID for missing partial on options classes.
    /// </summary>
    public const string DiagnosticId = "ASTAROPT002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Options class must be partial",
        "Options class '{0}' must be declared partial to support source generation",
        "AStar.Dev.Source.Analyzers",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

    /// <summary>
    /// Analyzes a type declaration for the [AutoRegisterOptions] attribute and missing partial keyword.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        if(context.Node is not TypeDeclarationSyntax typeDecl)
            return;
        INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken);
        if(symbol == null)
            return;
        foreach(AttributeData attr in symbol.GetAttributes())
        {
            if(attr.AttributeClass?.ToDisplayString() == "AStar.Dev.Source.Generators.Attributes.AutoRegisterOptionsAttribute")
            {
                if(!typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    var diag = Diagnostic.Create(Rule, typeDecl.Identifier.GetLocation(), symbol.Name);
                    context.ReportDiagnostic(diag);
                }

                break;
            }
        }
    }
}
