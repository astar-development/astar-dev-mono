using System.Text;
using AStarDev.SourceGenerators.StrongTypeCodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AStarDev.SourceGenerators.StrongTypeCodeGeneration;

/// <summary>The <see cref="StrongTypeGenerator" /> class is a source</summary>
[Generator]
public class StrongTypeGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The <see cref="Initialize" /> method is called by the compiler to register the source generation steps. It sets up a syntax provider to find all partial record structs with attributes and generates source code for those annotated with the <see cref="SourceGeneratorAttributes.StrongTypeAttribute" />.
    /// </summary>
    /// <param name="context"></param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var recordStructs = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is RecordDeclarationSyntax rec && rec.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
                transform: static (ctx, _) => (RecordDeclarationSyntax)ctx.Node)
            .Where(static rds => rds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
                                 rds.AttributeLists.Count > 0)
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(recordStructs), static (spc, source) =>
        {
            (var compilation, var structs) = source;
            var strongTypeAttrSymbol = compilation.GetTypeByMetadataName("AStarDev.SourceGeneratorAttributes.StrongTypeAttribute");
            if (strongTypeAttrSymbol == null)
                return;

            foreach (var recordStruct in structs)
            {
                var model = compilation.GetSemanticModel(recordStruct.SyntaxTree);
                if (model.GetDeclaredSymbol(recordStruct) is not { } symbol)
                    continue;

                var attr = symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, strongTypeAttrSymbol));
                if (attr == null)
                    continue;

                if (attr.ConstructorArguments.Length > 1)
                    continue;

                if (symbol.ContainingType is not null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "ASTARST001",
                            title: "Nested Strong Type",
                            messageFormat: $"Strong type '{symbol.Name}' is nested in '{symbol.ContainingType.Name}'; nested strong types are not supported. Declare it at namespace level.",
                            category: "AStarDev.SourceGenerators",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        recordStruct.GetLocation()));
                    continue;
                }

                string underlyingType = StrongTypeModelExtensions.CreateUnderlyingTypeFromAttribute(attr);
                string? ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
                var modelObj = new StrongTypeModel(ns, symbol.Name, symbol.DeclaredAccessibility, underlyingType);
                string code = StrongTypeCodeGenerator.Generate(modelObj);
                spc.AddSource($"{symbol.ToDisplayString()}_StrongType.generated.cs", SourceText.From(code, Encoding.UTF8));
            }
        });
    }
}
