using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStarDev.SourceAnalyzers.TestsUnit.Utilities;

internal static class CompilationHelpers
{
    private const string StrongTypeAttributeSource = @"using System;
namespace AStarDev.SourceGeneratorAttributes {
    public sealed class StrongTypeAttribute(Type? idType = null) : Attribute
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
    private const string AutoRegisterServiceAttributeSource = @"namespace AStarDev.SourceGeneratorAttributes {
    public enum ServiceLifetime
    {
        Singleton,
        Scoped,
        Transient
    }

    public enum Layer
    {
        Application,
        Domain,
        Infrastructure,
        Miscellaneous
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AutoRegisterServiceAttribute : Attribute
    {
        public AutoRegisterServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped, Layer layer = Layer.Miscellaneous)
        {
            Lifetime = lifetime;
            Layer = layer;
        }

        public ServiceLifetime Lifetime { get; }
        public Layer Layer { get; }
        public Type? As { get; set; }
        public bool AsSelf { get; set; }
    }
}
";

    public static CSharpCompilation CreateCompilation(string input)
        => CSharpCompilation.Create("TestAssembly",
            [
                CSharpSyntaxTree.ParseText(StrongTypeAttributeSource),
                CSharpSyntaxTree.ParseText(AutoRegisterOptionsAttributeSource),
                CSharpSyntaxTree.ParseText(AutoRegisterServiceAttributeSource),
                CSharpSyntaxTree.ParseText(input)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
