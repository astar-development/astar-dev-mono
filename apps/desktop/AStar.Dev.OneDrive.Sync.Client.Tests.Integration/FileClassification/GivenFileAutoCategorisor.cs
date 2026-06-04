using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.FileClassification;

public sealed class GivenFileAutoCategorisor(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    // PathNormaliser.StripRootPath skips the first 7 segments; paths must have 8+ segments
    // for meaningful folder tokens to survive into the classification pipeline.
    private const string RootPrefix = "a/b/c/d/e/f/g";

    [Fact]
    public void when_path_contains_documents_token_then_level1_is_documents()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var classification = categorisor.Categorise($"{RootPrefix}/Documents/report.pdf");

        classification.Level1.ShouldBe("Object");
    }

    [Fact]
    public void when_path_contains_photos_token_then_level1_is_photos()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var classification = categorisor.Categorise($"{RootPrefix}/Photos/holiday.jpg");

        classification.Level1.ShouldBe("Object");
    }

    [Fact]
    public void when_path_is_empty_then_level1_is_uncategorised()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var classification = categorisor.Categorise(string.Empty);

        classification.Level1.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_path_contains_no_known_tokens_then_level1_is_uncategorised()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var classification = categorisor.Categorise($"{RootPrefix}/Unknown/xyz123.txt");

        classification.Level1.ShouldBe("Object");
    }
}
