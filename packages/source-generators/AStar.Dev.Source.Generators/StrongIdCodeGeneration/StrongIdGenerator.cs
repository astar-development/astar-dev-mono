using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AStar.Dev.Source.Generators.StrongIdCodeGeneration;

[Generator]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1038:Compiler extensions should be implemented in assemblies with compiler-provided references", Justification = "<Pending>")]
public class StrongIdGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all readonly partial record structs with attributes
        IncrementalValueProvider<ImmutableArray<RecordDeclarationSyntax>> recordStructs = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is RecordDeclarationSyntax rec && rec.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
                transform: static (ctx, _) => (RecordDeclarationSyntax)ctx.Node)
            .Where(static rds => rds.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)) &&
                                 rds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
                                 rds.AttributeLists.Count > 0)
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(recordStructs), static (spc, source) =>
        {
            (Compilation? compilation, ImmutableArray<RecordDeclarationSyntax> structs) = source;
            // Cache attribute symbol lookup
            INamedTypeSymbol? strongIdAttrSymbol = compilation.GetTypeByMetadataName("AStar.Dev.Source.Generators.Attributes.StrongIdAttribute");
            if(strongIdAttrSymbol == null)
                return;

            foreach(RecordDeclarationSyntax? recordStruct in structs)
            {
                SemanticModel model = compilation.GetSemanticModel(recordStruct.SyntaxTree);
                if(model.GetDeclaredSymbol(recordStruct) is not INamedTypeSymbol symbol)
                    continue;

                AttributeData? attr = symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, strongIdAttrSymbol));
                if(attr == null)
                    continue;

                // Only allow 0 or 1 constructor argument
                if(attr.ConstructorArguments.Length > 1)
                    continue;

                // Use StrongIdModel logic for underlying type
                var underlyingType = StrongIdModelExtensions.CreateUnderlyingTypeFromAttribute(attr);
                var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
                var modelObj = new StrongIdModel(ns, symbol.Name, symbol.DeclaredAccessibility, underlyingType);
                var code = StrongIdCodeGenerator.Generate(modelObj);
                spc.AddSource($"{modelObj.ModelName}_StrongId.g.cs", SourceText.From(code, Encoding.UTF8));
            }
        });
    }
}
