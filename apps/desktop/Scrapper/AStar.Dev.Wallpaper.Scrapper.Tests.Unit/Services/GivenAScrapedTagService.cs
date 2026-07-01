using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;
using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAScrapedTagService
{
    private const string ActionTagValue = "Action";
    private const string GenreCategory  = "Genre";

    [Fact]
    public async Task when_exporting_then_repository_get_all_is_called()
    {
        var repo = Substitute.For<IScrapedTagRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var sut  = new ScrapedTagService(repo);

        await sut.ExportScrapedTagsAsync(CancellationToken.None);

        await repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_exporting_then_returned_tags_are_passed_through()
    {
        var repo     = Substitute.For<IScrapedTagRepository>();
        var expected = new List<ScrapedTagDomain> { new() { Value = ActionTagValue, Category = GenreCategory, IncludeInSearch = true } };
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expected);
        var sut      = new ScrapedTagService(repo);

        var result = await sut.ExportScrapedTagsAsync(CancellationToken.None);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task when_importing_new_tag_then_it_is_added()
    {
        var repo = Substitute.For<IScrapedTagRepository>();
        var sut  = new ScrapedTagService(repo);
        var tags = new List<ScrapedTagDomain> { new() { Value = ActionTagValue, Category = GenreCategory, IncludeInSearch = true } };

        await sut.ImportScrapedTagsAsync(tags, CancellationToken.None);

        await repo.Received(1).UpsertAsync(Arg.Is<IReadOnlyList<ScrapedTagDomain>>(list => list.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_a_tag_with_include_in_search_false_then_upsert_is_called_with_false_value()
    {
        var repo = Substitute.For<IScrapedTagRepository>();
        var sut  = new ScrapedTagService(repo);
        var tags = new List<ScrapedTagDomain> { new() { Value = ActionTagValue, Category = GenreCategory, IncludeInSearch = false } };

        await sut.ImportScrapedTagsAsync(tags, CancellationToken.None);

        await repo.Received(1).UpsertAsync(Arg.Is<IReadOnlyList<ScrapedTagDomain>>(list => !list[0].IncludeInSearch), Arg.Any<CancellationToken>());
    }
}
