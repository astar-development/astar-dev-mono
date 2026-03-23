using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AStar.Dev.Source.Generators.OptionsBindingGeneration;

[Generator]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1038:Compiler extensions should be implemented in assemblies with compiler-provided references", Justification = "<Pending>")]
public sealed partial class OptionsBindingGenerator : IIncrementalGenerator
{
    private const string AttrFqn = "AStar.Dev.Source.Generators.Attributes.AutoRegisterOptionsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<OptionsTypeInfo?>> optionsTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttrFqn,
            static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
            static (ctx, _) => GetOptionsTypeInfo(ctx)
        ).Collect();

        context.RegisterSourceOutput(optionsTypes, static (spc, types) =>
        {
            var validTypes = new List<OptionsTypeInfo>();
            foreach(OptionsTypeInfo? info in types)
            {
                if(info == null)
                    continue;
                if(string.IsNullOrWhiteSpace(info.SectionName))
                {
                    var diag = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "ASTAROPT001",
                            title: "Missing Section Name",
                            messageFormat: $"Options class '{info.TypeName}' must specify a section name via the attribute or a static SectionName const field.",
                            category: "AStar.Dev.Source.Generators",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        info.Location);
                    spc.ReportDiagnostic(diag);
                    continue;
                }

                validTypes.Add(info);
            }

            if(validTypes.Count == 0)
                return;
            var code = OptionsBindingCodeGenerator.Generate(validTypes);
            spc.AddSource("AutoOptionsRegistrationExtensions.g.cs", code);
        });
    }

    private static OptionsTypeInfo? GetOptionsTypeInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if(ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;
        var typeName = typeSymbol.Name;
        var ns = typeSymbol.ContainingNamespace?.ToDisplayString();
        var fullTypeName = ns != null ? string.Concat(ns, ".", typeName) : typeName;
        string? sectionName = null;
        AttributeData? attr = typeSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttrFqn);
        if(attr is { ConstructorArguments.Length: > 0 } && attr.ConstructorArguments[0].Value is string s && !string.IsNullOrWhiteSpace(s))
        {
            sectionName = s;
        }
        else if(ctx.Attributes.Length > 0)
        {
            // Fallback: parse from syntax
            var attrSyntax = ctx.Attributes[0].ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
            if(attrSyntax?.ArgumentList?.Arguments.Count > 0)
            {
                ExpressionSyntax expr = attrSyntax.ArgumentList.Arguments[0].Expression;
                if(expr is LiteralExpressionSyntax { Token.Value: string literalValue }) sectionName = literalValue;
            }
        }

        return !string.IsNullOrWhiteSpace(sectionName) 
            ? new OptionsTypeInfo(typeName, fullTypeName, sectionName!, ctx.TargetNode.GetLocation()) 
            : ExtractSectionNameFromMembers(ctx, typeSymbol, sectionName, typeName, fullTypeName);
    }

    private static OptionsTypeInfo? ExtractSectionNameFromMembers(GeneratorAttributeSyntaxContext ctx, INamedTypeSymbol typeSymbol, string? sectionName, string typeName, string fullTypeName)
    {
        foreach(ISymbol member in typeSymbol.GetMembers())
        {
            if(member is not IFieldSymbol { IsStatic: true, IsConst: true, Name: "SectionName" } field || field.Type.SpecialType != SpecialType.System_String ||
               field.ConstantValue is not string val || string.IsNullOrWhiteSpace(val))
            {
                continue;
            }

            sectionName = val;
            break;
        }

        return new OptionsTypeInfo(typeName, fullTypeName, sectionName ?? string.Empty, ctx.TargetNode.GetLocation());
    }
}
