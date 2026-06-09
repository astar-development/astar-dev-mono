using AStar.Dev.Functional.Extensions;
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
    public void when_path_contains_documents_token_then_result_is_none()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var result = categorisor.Categorise($"{RootPrefix}/Documents/report.pdf");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_path_contains_photos_token_with_no_colour_or_person_then_result_is_none()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var result = categorisor.Categorise($"{RootPrefix}/Photos/holiday.jpg");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_path_is_empty_then_result_is_none()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var result = categorisor.Categorise(string.Empty);

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_path_contains_no_known_tokens_then_result_is_none()
    {
        var categorisor = fixture.Services.GetRequiredService<IFileAutoCategorisor>();

        var result = categorisor.Categorise($"{RootPrefix}/Unknown/xyz123.txt");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }
}
