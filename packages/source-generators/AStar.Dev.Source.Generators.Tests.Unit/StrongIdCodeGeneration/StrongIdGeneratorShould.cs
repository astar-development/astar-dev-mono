using System.Linq;
using AStar.Dev.Source.Generators.StrongIdCodeGeneration;
using AStar.Dev.Source.Generators.Tests.Unit.Utilitites;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStar.Dev.Source.Generators.Tests.Unit.StrongIdCodeGeneration;

public class StrongIdGeneratorShould
{
    [Fact]
    public void GeneratePartialStructWithIdPropertyWithTypeOfIntWhenSpecifiedForValidReadonlyRecordStruct()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(int))]
    public readonly partial record struct MyId { }
}";

        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Int32 Id)");
    }

    [Fact]
    public void GeneratePartialStructWithIdPropertyWithTypeOfStringWhenSpecifiedForValidReadonlyRecordStruct()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(string))]
    public readonly partial record struct MyId { }
}";

        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.String Id)");
    }

    [Fact]
    public void GeneratePartialStructWithIdPropertyWithTypeOfGuidWhenSpecifiedForValidReadonlyRecordStruct()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(Guid))]
    public readonly partial record struct MyId { }
}";

        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Guid Id)");
    }

    [Fact]
    public void GeneratePartialStructWithIdPropertyWithDefaultTypeOfGuidWhenNotSpecifiedForValidReadonlyRecordStruct()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId]
    public readonly partial record struct MyId { }
}";

        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        var generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Guid Id);");
    }

    [Fact]
    public void DoesNotGenerate_ForNonReadonlyOrNonRecordStruct()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(int))]
    public partial struct MyId { }
}";

        CSharpCompilation compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        GeneratedSourceResult generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId"));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeTrue();
    }
}
