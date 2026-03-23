using System.Linq;
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
        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();

        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
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
        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
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
        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
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
        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
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
        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
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
        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("AutoOptionsRegistrationExtensions"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
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
        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);
        var generator = new OptionsBindingGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        if(allGenerated.Count > 0)
        {
            var generatedText = allGenerated[0].SourceText.ToString();
            generatedText.ShouldNotContain("NotRegistered");
        }
    }
}
