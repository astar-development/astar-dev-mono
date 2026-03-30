using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AStar.Dev.Source.Generators.StrongIdCodeGeneration;

/// <summary>
///   The <see cref="StrongIdGenerator" /> class is a source
/// </summary>
[Generator]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1038:Compiler extensions should be implemented in assemblies with compiler-provided references", Justification = "<Pending>")]
public class StrongIdGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The <see cref="Initialize" /> method is called by the compiler to register the source generation steps. It sets up a syntax provider to find all readonly partial record structs with attributes and generates source code for those annotated with the <see cref="Attributes.StrongIdAttribute" />.
    /// </summary>
    /// <param name="context"></param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all readonly partial record structs with attributes
        var recordStructs = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is RecordDeclarationSyntax rec && rec.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
                transform: static (ctx, _) => (RecordDeclarationSyntax)ctx.Node)
            .Where(static rds => rds.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)) &&
                                 rds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
                                 rds.AttributeLists.Count > 0)
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(recordStructs), static (spc, source) =>
        {
            (var compilation, var structs) = source;
            // Cache attribute symbol lookup
            var strongIdAttrSymbol = compilation.GetTypeByMetadataName("AStar.Dev.Source.Generators.Attributes.StrongIdAttribute");
            if(strongIdAttrSymbol == null)
                return;

            foreach(var recordStruct in structs)
            {
                var model = compilation.GetSemanticModel(recordStruct.SyntaxTree);
                if(model.GetDeclaredSymbol(recordStruct) is not INamedTypeSymbol symbol)
                    continue;

                var attr = symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, strongIdAttrSymbol));
                if(attr == null)
                    continue;

                // Only allow 0 or 1 constructor argument
                if(attr.ConstructorArguments.Length > 1)
                    continue;

                // Use StrongIdModel logic for underlying type
                string underlyingType = StrongIdModelExtensions.CreateUnderlyingTypeFromAttribute(attr);
                string? ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
                var modelObj = new StrongIdModel(ns, symbol.Name, symbol.DeclaredAccessibility, underlyingType);
                string code = StrongIdCodeGenerator.Generate(modelObj);
                spc.AddSource($"{modelObj.ModelName}_StrongId.g.cs", SourceText.From(code, Encoding.UTF8));
            }
        });
    }
}
