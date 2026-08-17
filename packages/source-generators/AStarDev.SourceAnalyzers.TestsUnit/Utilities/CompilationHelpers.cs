using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStarDev.SourceAnalyzers.TestsUnit.Utilities;

internal static class CompilationHelpers
{
    private const string StrongIdAttributeSource = @"using System;
namespace AStarDev.SourceGeneratorAttributes {
    public sealed class StrongIdAttribute(Type? idType = null) : Attribute
    {
        public Type IdType { get; } = idType ?? typeof(Guid);
    }
}";
    private const string AutoRegisterOptionsAttributeSource = @"namespace AStarDev.SourceGeneratorAttributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class AutoRegisterOptionsAttribute : Attribute
{
    public AutoRegisterOptionsAttribute(string sectionName)
    {
        SectionName = sectionName;
    }

    public string? SectionName { get; }
}
";

    public static CSharpCompilation CreateCompilation(string input)
        => CSharpCompilation.Create("TestAssembly",
            [
                CSharpSyntaxTree.ParseText(StrongIdAttributeSource),
                CSharpSyntaxTree.ParseText(AutoRegisterOptionsAttributeSource),
                CSharpSyntaxTree.ParseText(input)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
