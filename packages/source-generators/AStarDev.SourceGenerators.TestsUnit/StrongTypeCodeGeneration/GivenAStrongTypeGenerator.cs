using AStarDev.SourceGenerators.StrongTypeCodeGeneration;
using AStarDev.SourceGenerators.TestsUnit.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStarDev.SourceGenerators.TestsUnit.StrongTypeCodeGeneration;

public sealed class GivenAStrongTypeGenerator
{
    [Fact]
    public void when_the_type_is_int_on_a_partial_record_struct_then_the_full_record_struct_is_generated_with_id_property_of_type_int()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Int32 Value)");
    }

    [Fact]
    public void when_the_type_is_string_on_a_partial_record_struct_then_the_full_record_struct_is_generated_with_id_property_of_type_string()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(string))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.String Value)");
    }

    [Fact]
    public void when_the_type_is_guid_on_a_partial_record_struct_then_the_full_record_struct_is_generated_with_id_property_of_type_guid()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(Guid))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Guid Value)");
    }

    [Fact]
    public void when_the_type_is_long_on_a_partial_record_struct_then_the_full_record_struct_is_generated_with_id_property_of_type_long()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(long))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Int64 Value)");
    }

    [Fact]
    public void when_no_type_is_specified_on_a_partial_record_struct_then_the_full_record_struct_is_generated_with_id_property_of_type_guid()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Guid Value)");
    }

    [Fact]
    public void when_the_full_record_struct_is_generated_with_id_property_then_the_implicit_converters_should_be_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldBe("""
                               // <auto-generated/>

                               using System;
                               using System.Collections.Generic;

                               namespace TestNamespace;

                               /// <summary>A strongly-typed identifier wrapping a <see cref="System.Guid"/> value.</summary>
                               /// <param name="Value">The underlying identifier value.</param>
                               public readonly partial record struct MyId(System.Guid Value)
                               {
                                   /// <inheritdoc />
                                   public static implicit operator MyId(System.Guid value) => new MyId(value);

                                   /// <inheritdoc />
                                   public static implicit operator System.Guid(MyId id) => id.Value;
                               }
                               """);
    }

    [Fact]
    public void when_the_partial_record_struct_is_not_readonly_then_the_full_record_struct_is_still_generated_with_readonly_added()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Int32 Value)");
    }

    [Fact]
    public void when_the_type_is_non_partial_record_struct_then_no_code_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeTrue();
    }

    [Fact]
    public void when_the_type_is_non_partial_non_record_struct_then_no_code_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongTypeGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeTrue();
    }

    private static GeneratorDriverRunResult RunGenerator(string input)
        => GeneratorRunHelpers.Run(new StrongTypeGenerator(), CompilationHelpers.CreateCompilation(input));

    [Fact]
    public void when_the_type_is_internal_then_the_generated_type_is_internal()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 internal readonly partial record struct MyId { }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratorRunHelpers.GeneratedSources(result).Single().SourceText.ToString().ShouldContain("internal readonly partial record struct MyId(System.Int32 Value)");
    }

    [Fact]
    public void when_the_type_is_in_the_global_namespace_then_no_namespace_declaration_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;

                             [StrongType(typeof(int))]
                             public readonly partial record struct GlobalId { }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);
        var result = GeneratorRunHelpers.Run(new StrongTypeGenerator(), compilation);

        string text = GeneratorRunHelpers.GeneratedSources(result).Single().SourceText.ToString();
        text.ShouldNotContain("namespace");
        text.ShouldContain("public readonly partial record struct GlobalId(System.Int32 Value)");
        GeneratorRunHelpers.CompilationErrorsInGeneratedCode(compilation, result).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_type_is_declared_then_the_hint_name_identifies_the_strong_type()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratorRunHelpers.GeneratedSources(result).Single().HintName.ShouldBe("TestNamespace.MyId_StrongType.generated.cs");
    }

    [Fact]
    public void when_the_attribute_argument_is_null_then_the_underlying_type_defaults_to_guid()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(null)]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratorRunHelpers.GeneratedSources(result).Single().SourceText.ToString().ShouldContain("public readonly partial record struct MyId(System.Guid Value)");
    }

    [Fact]
    public void when_the_attribute_has_empty_arguments_then_the_underlying_type_defaults_to_guid()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType()]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratorRunHelpers.GeneratedSources(result).Single().SourceText.ToString().ShouldContain("public readonly partial record struct MyId(System.Guid Value)");
    }

    [Fact]
    public void when_multiple_strong_types_are_declared_then_a_source_is_generated_for_each()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public readonly partial record struct FirstId { }

                                 [StrongType(typeof(string))]
                                 public readonly partial record struct SecondId { }
                             }
                             """;

        var result = RunGenerator(input);

        var sources = GeneratorRunHelpers.GeneratedSources(result);
        sources.Length.ShouldBe(2);
        sources.ShouldContain(source => source.SourceText.ToString().Contains("FirstId(System.Int32 Value)", StringComparison.Ordinal));
        sources.ShouldContain(source => source.SourceText.ToString().Contains("SecondId(System.String Value)", StringComparison.Ordinal));
    }

    [Fact]
    public void when_two_strong_types_share_a_name_in_different_namespaces_then_both_are_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace FirstNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public readonly partial record struct MyId { }
                             }
                             namespace SecondNamespace
                             {
                                 [StrongType(typeof(long))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldBeEmpty();
        GeneratorRunHelpers.GeneratedSources(result).Length.ShouldBe(2);
    }

    [Fact]
    public void when_a_strong_type_is_generated_then_the_combined_compilation_has_no_errors_in_the_generated_code()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);
        var result = GeneratorRunHelpers.Run(new StrongTypeGenerator(), compilation);

        GeneratorRunHelpers.CompilationErrorsInGeneratedCode(compilation, result).ShouldBeEmpty();
        GeneratorRunHelpers.DocumentationDiagnostics(compilation, GeneratorRunHelpers.GeneratedSources(result).Single().SourceText.ToString()).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_record_struct_is_nested_in_another_type_then_a_diagnostic_is_emitted_and_nothing_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 public partial class Outer
                                 {
                                     [StrongType(typeof(int))]
                                     public partial record struct InnerId { }
                                 }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTARST001" && diagnostic.Severity == DiagnosticSeverity.Error);
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_record_struct_is_nested_more_than_one_level_deep_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 public partial class Outer
                                 {
                                     public partial struct Middle
                                     {
                                         [StrongType(typeof(int))]
                                         public partial record struct InnerId { }
                                     }
                                 }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTARST001");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_nested_record_struct_is_next_to_a_top_level_one_then_only_the_top_level_one_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public partial record struct TopLevelId { }

                                 public partial class Outer
                                 {
                                     [StrongType(typeof(int))]
                                     public partial record struct InnerId { }
                                 }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTARST001");
        var generated = GeneratorRunHelpers.GeneratedSources(result).ShouldHaveSingleItem().SourceText.ToString();
        generated.ShouldContain("TopLevelId");
        generated.ShouldNotContain("InnerId");
    }

    [Fact]
    public void when_the_type_is_a_partial_record_class_then_no_code_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public partial record MyId { }
                             }
                             """;

        GeneratorRunHelpers.GeneratedSources(RunGenerator(input)).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_type_is_a_partial_struct_that_is_not_a_record_then_no_code_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [StrongType(typeof(int))]
                                 public partial struct MyId { }
                             }
                             """;

        GeneratorRunHelpers.GeneratedSources(RunGenerator(input)).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_partial_record_struct_has_only_other_attributes_then_no_code_is_generated()
    {
        const string input = """
                             using System;
                             namespace TestNamespace
                             {
                                 [Obsolete]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        GeneratorRunHelpers.GeneratedSources(RunGenerator(input)).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_partial_record_struct_has_no_attributes_then_no_code_is_generated()
    {
        const string input = """
                             namespace TestNamespace
                             {
                                 public readonly partial record struct MyId { }
                             }
                             """;

        GeneratorRunHelpers.GeneratedSources(RunGenerator(input)).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_strong_type_attribute_is_not_part_of_the_compilation_then_nothing_is_generated_and_no_error_is_reported()
    {
        var compilation = CSharpCompilation.Create("TestAssembly",
            [CSharpSyntaxTree.ParseText("namespace TestNamespace { [Foo] public readonly partial record struct MyId { } }", cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var result = GeneratorRunHelpers.Run(new StrongTypeGenerator(), compilation);

        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
        result.Diagnostics.ShouldBeEmpty();
    }
}
