using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.Services;
using ScrapedTagDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenAFileClassificationCategoryService
{
    private const string ActionTagValue = "Action";
    private const int ParentId = 1;

    [Fact]
    public async Task when_exporting_then_repository_get_all_is_called()
    {
        var repo = Substitute.For<IFileClassificationCategoriesRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result.Success<List<ScrapedTagDomain>, ScrapeError>([]));
        var sut = new FileClassificationCategoryService(repo);

        await sut.ExportScrapedTagsAsync(CancellationToken.None);

        await repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_exporting_then_returned_tags_are_passed_through()
    {
        var repo = Substitute.For<IFileClassificationCategoriesRepository>();
        var expected = new List<ScrapedTagDomain> { new() { Name = ActionTagValue, ParentId = ParentId, IncludeInSearch = true } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result.Success<List<ScrapedTagDomain>, ScrapeError>(expected));
        var sut = new FileClassificationCategoryService(repo);

        var result = await sut.ExportScrapedTagsAsync(CancellationToken.None);

        result.ShouldBeOfType<Ok<List<ScrapedTagDomain>, ScrapeError>>().Value.ShouldBe(expected);
    }

    [Fact]
    public async Task when_importing_new_tag_then_it_is_added()
    {
        var repo = Substitute.For<IFileClassificationCategoriesRepository>();
        repo.UpsertAsync(Arg.Any<IReadOnlyList<ScrapedTagDomain>>(), Arg.Any<CancellationToken>()).Returns(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Instance));
        var sut = new FileClassificationCategoryService(repo);
        var tags = new List<ScrapedTagDomain> { new() { Name = ActionTagValue, ParentId = ParentId, IncludeInSearch = true } };

        await sut.ImportScrapedTagsAsync(tags, CancellationToken.None);

        await repo.Received(1).UpsertAsync(Arg.Is<IReadOnlyList<ScrapedTagDomain>>(list => list.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_a_tag_with_include_in_search_false_then_upsert_is_called_with_false_value()
    {
        var repo = Substitute.For<IFileClassificationCategoriesRepository>();
        repo.UpsertAsync(Arg.Any<IReadOnlyList<ScrapedTagDomain>>(), Arg.Any<CancellationToken>()).Returns(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Instance));
        var sut = new FileClassificationCategoryService(repo);
        var tags = new List<ScrapedTagDomain> { new() { Name = ActionTagValue, ParentId = ParentId, IncludeInSearch = false } };

        await sut.ImportScrapedTagsAsync(tags, CancellationToken.None);

        await repo.Received(1).UpsertAsync(Arg.Is<IReadOnlyList<ScrapedTagDomain>>(list => !list[0].IncludeInSearch), Arg.Any<CancellationToken>());
    }
}
