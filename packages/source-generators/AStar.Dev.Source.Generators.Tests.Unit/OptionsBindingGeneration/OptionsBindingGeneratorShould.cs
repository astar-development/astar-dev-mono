using AStar.Dev.Source.Generators.OptionsBindingGeneration;
using AStar.Dev.Source.Generators.Tests.Unit.Utilitites;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStar.Dev.Source.Generators.Tests.Unit.OptionsBindingGeneration;

public class OptionsBindingGeneratorShould
{
    [Fact]
    public void GenerateRegistrationForClassWithAttributeSectionName()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace TestNamespace
{
    [AutoRegisterOptions(""MySection"")]
    public partial class MyOptions { }
}";
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        CSharpGeneratorDriver? driver = CSharpGeneratorDriver.Create(generator);
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
    public void GenerateRegistrationForStructWithAttributeSectionName()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace TestNamespace
{
    [AutoRegisterOptions(""StructSection"")]
    public partial struct MyStructOptions { }
}";
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        CSharpGeneratorDriver? driver = CSharpGeneratorDriver.Create(generator);
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
    public void GenerateRegistrationForClassWithConstSectionNameField()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace TestNamespace
{
    [AutoRegisterOptions]
    public partial class MyOptionsWithField { public const string SectionName = ""FieldSection""; }
}";
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        CSharpGeneratorDriver? driver = CSharpGeneratorDriver.Create(generator);
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
    public void PreferAttributeSectionNameOverField()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace TestNamespace
{
    [AutoRegisterOptions(""AttrSection"")]
    public partial class MyOptionsWithBoth { public const string SectionName = ""FieldSection""; }
}";
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        CSharpGeneratorDriver? driver = CSharpGeneratorDriver.Create(generator);
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
    public void EmitDiagnosticIfNoSectionName()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace TestNamespace
{
    [AutoRegisterOptions]
    public partial class MyOptionsNoSection { }
}";
        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new OptionsBindingGenerator();
        CSharpGeneratorDriver? driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        result.Diagnostics.ShouldContain(d => d.Id == "ASTAROPT001");
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        allGenerated.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRegistrationsForMultipleTypes()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace TestNamespace
{
    [AutoRegisterOptions(""SectionA"")]
    public partial class OptionsA { }
    [AutoRegisterOptions]
    public partial class OptionsB { public const string SectionName = ""SectionB""; }
}";
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        CSharpGeneratorDriver? driver = CSharpGeneratorDriver.Create(generator);
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
    public void DoesNotGenerateForTypesWithoutAttribute()
    {
        const string input = @"using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
namespace TestNamespace
{
    public partial class NotRegistered { public const string SectionName = ""SectionX""; }
}";
        var compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        CSharpGeneratorDriver? driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        if(allGenerated.Count > 0)
        {
            string generatedText = allGenerated[0].SourceText.ToString();
            generatedText.ShouldNotContain("NotRegistered");
        }
    }
}
