using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AStar.Dev.Source.Generators.ServiceRegistrationGeneration;

[Generator]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1038:Compiler extensions should be implemented in assemblies with compiler-provided references", Justification = "<Pending>")]
public sealed partial class ServiceRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol?> classSyntax = CreateClassSyntaxProvider(context);
        IncrementalValuesProvider<(INamedTypeSymbol sym, AttributeData? attr)> services = CreateServicesProvider(classSyntax);
        IncrementalValuesProvider<ServiceModel?> serviceModels = CreateServiceModelsProvider(services);
        IncrementalValueProvider<(Compilation Left, ImmutableArray<ServiceModel?> Right)> combined = context.CompilationProvider.Combine(serviceModels.Collect());

        context.RegisterSourceOutput(combined, GenerateSource);
    }

    private static IncrementalValuesProvider<INamedTypeSymbol?> CreateClassSyntaxProvider(IncrementalGeneratorInitializationContext ctx)
        => ctx.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => IsClassCandidateForServiceRegistration(node),
                transform: static (syntaxCtx, _) => GetDeclaredSymbol(syntaxCtx))
            .Where(static s => s is not null)!;

    private static bool IsClassCandidateForServiceRegistration(SyntaxNode node)
        => node is ClassDeclarationSyntax c &&
                       c.AttributeLists.Count > 0 &&
                       c.TypeParameterList is null;

    private static INamedTypeSymbol? GetDeclaredSymbol(GeneratorSyntaxContext syntaxCtx)
    {
        var classDecl = (ClassDeclarationSyntax)syntaxCtx.Node;

        return syntaxCtx.SemanticModel.GetDeclaredSymbol(classDecl);
    }

    private static IncrementalValuesProvider<(INamedTypeSymbol sym, AttributeData? attr)> CreateServicesProvider(IncrementalValuesProvider<INamedTypeSymbol?> classSyntax)
        => classSyntax
                .Select(static (sym, _) => (sym, attr: FindServiceAttribute(sym!)))
                .Where(static t => t.attr is not null)!;

    private static AttributeData? FindServiceAttribute(INamedTypeSymbol symbol)
        => symbol.GetAttributes()
                .FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() == "AStar.Dev.Source.Generators.Attributes.AutoRegisterServiceAttribute");

    private static IncrementalValuesProvider<ServiceModel?> CreateServiceModelsProvider(
        IncrementalValuesProvider<(INamedTypeSymbol sym, AttributeData? attr)> services)
    {
        IncrementalValuesProvider<ServiceModel?> result = services
            .Select(static (t, _) => ServiceModel.TryCreate(t.sym, t.attr!))
            .Where(static m => m is not null)!;

        return result;  // dont see anything here
    }

    private static void GenerateSource(SourceProductionContext spc, (Compilation Left, ImmutableArray<ServiceModel?> Right) pair)
    {
        var code = ServiceCollectionCodeGenerator.Generate(pair.Right);
        spc.AddSource("GeneratedServiceCollectionExtensions.g.cs", code);
    }
}
