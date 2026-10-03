using AStarDev.SourceGenerators.OptionsBindingGeneration;
using AStarDev.SourceGenerators.TestsUnit.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStarDev.SourceGenerators.TestsUnit.OptionsBindingGeneration;

public sealed class GivenAnOptionsBindingGenerator
{
    [Fact]
    public void when_a_class_has_an_attribute_section_name_then_registration_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             using Microsoft.Extensions.DependencyInjection;
                             using Microsoft.Extensions.Configuration;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("MySection")]
                                 public partial class MyOptions { }
                             }
                             """;
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();

        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("services.AddOptions<TestNamespace.MyOptions>()");
        generatedText.ShouldContain(".Bind(configuration.GetSection(\"MySection\"))");
    }

    [Fact]
    public void when_a_struct_has_an_attribute_section_name_then_registration_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             using Microsoft.Extensions.DependencyInjection;
                             using Microsoft.Extensions.Configuration;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("StructSection")]
                                 public partial struct MyStructOptions { }
                             }
                             """;
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("services.AddOptions<TestNamespace.MyStructOptions>()");
        generatedText.ShouldContain(".Bind(configuration.GetSection(\"StructSection\"))");
    }

    [Fact]
    public void when_a_class_has_a_const_section_name_field_then_registration_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             using Microsoft.Extensions.DependencyInjection;
                             using Microsoft.Extensions.Configuration;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions]
                                 public partial class MyOptionsWithField { public const string SectionName = "FieldSection"; }
                             }
                             """;
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("services.AddOptions<TestNamespace.MyOptionsWithField>()");
        generatedText.ShouldContain(".Bind(configuration.GetSection(\"FieldSection\"))");
    }

    [Fact]
    public void when_a_class_has_both_an_attribute_section_name_and_a_field_then_the_attribute_section_name_is_preferred()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             using Microsoft.Extensions.DependencyInjection;
                             using Microsoft.Extensions.Configuration;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("AttrSection")]
                                 public partial class MyOptionsWithBoth { public const string SectionName = "FieldSection"; }
                             }
                             """;
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("services.AddOptions<TestNamespace.MyOptionsWithBoth>()");
        generatedText.ShouldContain(".Bind(configuration.GetSection(\"AttrSection\"))");
        generatedText.ShouldNotContain("FieldSection");
    }

    [Fact]
    public void when_a_class_has_no_section_name_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             using Microsoft.Extensions.DependencyInjection;
                             using Microsoft.Extensions.Configuration;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions]
                                 public partial class MyOptionsNoSection { }
                             }
                             """;
        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldContain(d => d.Id == "ASTAROPT001");
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        allGenerated.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_generic_class_is_annotated_then_a_diagnostic_is_emitted_and_nothing_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("GenericSection")]
                                 public partial class GenericOptions<TValue> { }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT002" && diagnostic.Severity == DiagnosticSeverity.Error);
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_generic_struct_is_annotated_then_a_diagnostic_is_emitted_and_nothing_is_generated()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("GenericSection")]
                                 public partial struct GenericOptions<TFirst, TSecond> { }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT002");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_generic_class_is_annotated_next_to_a_valid_one_then_only_the_valid_one_is_registered()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("GenericSection")]
                                 public partial class GenericOptions<TValue> { }

                                 [AutoRegisterOptions("PlainSection")]
                                 public partial class PlainOptions { }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT002");
        var generated = GeneratorRunHelpers.GeneratedSources(result).ShouldHaveSingleItem().SourceText.ToString();
        generated.ShouldContain("TestNamespace.PlainOptions");
        generated.ShouldNotContain("GenericOptions");
    }

    [Fact]
    public void when_a_class_is_nested_in_a_generic_class_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 public partial class Outer<TValue>
                                 {
                                     [AutoRegisterOptions("NestedSection")]
                                     public partial class Inner { }
                                 }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT002");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_multiple_types_are_annotated_then_registrations_are_generated_for_each()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             using Microsoft.Extensions.DependencyInjection;
                             using Microsoft.Extensions.Configuration;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("SectionA")]
                                 public partial class OptionsA { }
                                 [AutoRegisterOptions]
                                 public partial class OptionsB { public const string SectionName = "SectionB"; }
                             }
                             """;
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("services.AddOptions<TestNamespace.OptionsA>()");
        generatedText.ShouldContain(".Bind(configuration.GetSection(\"SectionA\"))");
        generatedText.ShouldContain("services.AddOptions<TestNamespace.OptionsB>()");
        generatedText.ShouldContain(".Bind(configuration.GetSection(\"SectionB\"))");
    }

    [Fact]
    public void when_a_type_has_no_attribute_then_no_registration_is_generated()
    {
        const string input = """
                             using Microsoft.Extensions.DependencyInjection;
                             using Microsoft.Extensions.Configuration;
                             namespace TestNamespace
                             {
                                 public partial class NotRegistered { public const string SectionName = "SectionX"; }
                             }
                             """;
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        if (allGenerated.Count > 0)
        {
            string generatedText = allGenerated[0].SourceText.ToString();
            generatedText.ShouldNotContain("NotRegistered");
        }
    }

    private static GeneratorDriverRunResult RunGenerator(string input)
        => GeneratorRunHelpers.Run(new OptionsBindingGenerator(), CompilationHelpers.CreateCompilation(input));

    private static string GeneratedText(GeneratorDriverRunResult result)
        => GeneratorRunHelpers.GeneratedSources(result).Single(x => x.HintName.Contains("AutoOptionsRegistrationExtensions", StringComparison.Ordinal)).SourceText.ToString();

    [Fact]
    public void when_a_section_name_is_an_empty_string_and_there_is_no_const_field_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("")]
                                 public partial class EmptySection { }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT001");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_section_name_is_whitespace_and_there_is_no_const_field_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("   ")]
                                 public partial class WhitespaceSection { }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT001");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_section_name_is_whitespace_and_a_const_field_exists_then_the_const_field_is_used()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("   ")]
                                 public partial class WhitespaceWithField { public const string SectionName = "FieldSection"; }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "ASTAROPT001");
        GeneratedText(result).ShouldContain(".Bind(configuration.GetSection(\"FieldSection\"))");
    }

    [Fact]
    public void when_the_const_section_name_field_is_empty_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions]
                                 public partial class EmptyConst { public const string SectionName = ""; }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT001");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_section_name_field_is_static_readonly_rather_than_const_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions]
                                 public partial class StaticReadonlyField { public static readonly string SectionName = "FieldSection"; }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT001");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_section_name_const_is_not_a_string_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions]
                                 public partial class IntConst { public const int SectionName = 42; }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT001");
        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_section_name_field_has_a_different_name_then_a_diagnostic_is_emitted()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions]
                                 public partial class WrongFieldName { public const string Section = "FieldSection"; }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT001");
    }

    [Fact]
    public void when_a_diagnostic_is_emitted_then_it_is_an_error_naming_the_type_and_located_on_the_declaration()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions]
                                 public partial class MyOptionsNoSection { }
                             }
                             """;

        var result = RunGenerator(input);

        var diagnostic = result.Diagnostics.Single(item => item.Id == "ASTAROPT001");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.GetMessage().ShouldContain("MyOptionsNoSection");
        diagnostic.Location.SourceTree.ShouldNotBeNull();
        diagnostic.Location.SourceTree.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan).ShouldContain("MyOptionsNoSection");
    }

    [Fact]
    public void when_valid_and_invalid_types_are_mixed_then_valid_types_are_registered_and_invalid_types_are_diagnosed()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("Good")]
                                 public partial class GoodOptions { }

                                 [AutoRegisterOptions]
                                 public partial class BadOptions { }
                             }
                             """;

        var result = RunGenerator(input);

        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "ASTAROPT001" && diagnostic.GetMessage().Contains("BadOptions", StringComparison.Ordinal));
        string text = GeneratedText(result);
        text.ShouldContain("services.AddOptions<TestNamespace.GoodOptions>()");
        text.ShouldNotContain("BadOptions");
    }

    [Fact]
    public void when_a_section_name_contains_quotes_and_backslashes_then_they_are_escaped_in_the_generated_code()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("A\"B\\C")]
                                 public partial class EscapedOptions { }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratedText(result).ShouldContain("GetSection(\"A\\\"B\\\\C\")");
    }

    [Fact]
    public void when_a_type_is_in_the_global_namespace_then_it_is_registered_without_a_namespace_prefix()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;

                             [AutoRegisterOptions("GlobalSection")]
                             public partial class GlobalOptions { }
                             """;

        var result = RunGenerator(input);

        string text = GeneratedText(result);
        text.ShouldContain("services.AddOptions<GlobalOptions>()");
        text.ShouldNotContain("global namespace");
    }

    [Fact]
    public void when_a_type_is_nested_then_the_containing_type_is_part_of_the_registered_type_name()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 public static class Outer
                                 {
                                     [AutoRegisterOptions("NestedSection")]
                                     public partial class NestedOptions { }
                                 }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratedText(result).ShouldContain("services.AddOptions<TestNamespace.Outer.NestedOptions>()");
    }

    [Fact]
    public void when_a_type_is_registered_then_the_full_extension_method_is_generated_with_validation()
    {
        const string input = """
                             using AStarDev.SourceGeneratorAttributes;
                             namespace TestNamespace
                             {
                                 [AutoRegisterOptions("MySection")]
                                 public partial class MyOptions { }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratedText(result).ShouldBe("""
                                       // <auto-generated/>
                                       using Microsoft.Extensions.DependencyInjection;
                                       using Microsoft.Extensions.Configuration;

                                       namespace AStarDev.SourceGenerators.OptionsBindingGeneration
                                       {
                                           public static class AutoOptionsRegistrationExtensions
                                           {
                                               public static IServiceCollection AddAutoRegisteredOptions(this IServiceCollection services, IConfiguration configuration)
                                               {
                                                   services.AddOptions<TestNamespace.MyOptions>()
                                                       .Bind(configuration.GetSection("MySection"))
                                                       .ValidateDataAnnotations()
                                                       .ValidateOnStart();
                                                   return services;
                                               }
                                           }
                                       }

                                       """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void when_a_type_has_other_attributes_but_not_the_options_attribute_then_no_registration_is_generated()
    {
        const string input = """
                             using System;
                             namespace TestNamespace
                             {
                                 [Obsolete]
                                 public partial class OtherAttribute { public const string SectionName = "SectionX"; }
                             }
                             """;

        var result = RunGenerator(input);

        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_compilation_has_no_annotated_types_then_nothing_is_generated_and_no_diagnostics_are_reported()
    {
        var result = RunGenerator("namespace TestNamespace { public class Plain { } }");

        GeneratorRunHelpers.GeneratedSources(result).ShouldBeEmpty();
        result.Diagnostics.ShouldBeEmpty();
    }
}
