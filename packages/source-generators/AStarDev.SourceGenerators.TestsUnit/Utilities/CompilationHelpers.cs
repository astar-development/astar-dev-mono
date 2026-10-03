using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStarDev.SourceGenerators.TestsUnit.Utilities;

internal static class CompilationHelpers
{
    private const string StrongTypeAttributeSource = """
                                                   using System;
                                                   namespace AStarDev.SourceGeneratorAttributes {
                                                       public sealed class StrongTypeAttribute(Type? idType = null) : Attribute
                                                       {
                                                           /// <summary>The type of the ID property (e.g., typeof(Guid), typeof(int)).</summary>
                                                           public Type IdType { get; } = idType ?? typeof(Guid);
                                                       }
                                                   }
                                                   """;
    private const string AutoRegisterOptionsAttributeSource = """
                                                              using System;

                                                              namespace AStarDev.SourceGeneratorAttributes;

                                                              [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
                                                              public class AutoRegisterOptionsAttribute : Attribute
                                                              {
                                                                  public AutoRegisterOptionsAttribute(string? sectionName = null)
                                                                  {
                                                                      SectionName = sectionName;
                                                                  }

                                                                  /// <summary>
                                                                  /// Gets the name of the configuration section associated with this instance.
                                                                  /// When not set, the section name defaults to the class or struct name.
                                                                  /// </summary>
                                                                  public string? SectionName { get; }
                                                              }

                                                              """;

    public static CSharpCompilation CreateCompilation(string input)
        => CSharpCompilation.Create("TestAssembly",
            [
                CSharpSyntaxTree.ParseText(StrongTypeAttributeSource),
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
