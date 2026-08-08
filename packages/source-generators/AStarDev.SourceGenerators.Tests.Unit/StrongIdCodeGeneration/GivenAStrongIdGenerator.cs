using AStarDev.SourceGenerators.StrongIdCodeGeneration;
using AStarDev.SourceGenerators.Tests.Unit.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStarDev.SourceGenerators.Tests.Unit.StrongIdCodeGeneration;

public sealed class GivenAStrongIdGenerator
{
    [Fact]
    public void when_the_type_is_int_on_a_partial_record_struct_then_the_full_record_struct_is_generated_with_id_property_of_type_int()
    {
        const string input = """
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId(typeof(int))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId(typeof(string))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId(typeof(Guid))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId(typeof(long))]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId]
                                 public readonly partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId(typeof(int))]
                                 public partial record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId(typeof(int))]
                                 public record struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
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
                             using AStarDev.SourceGenerators.Attributes;
                             namespace TestNamespace
                             {
                                 [StrongId(typeof(int))]
                                 public struct MyId { }
                             }
                             """;

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeTrue();
    }
}
