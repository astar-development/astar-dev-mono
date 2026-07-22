using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.TestData;

internal static class RepositoryTestDoubles
{
    internal static IFileDetailRepository BuildFileDetailRepository(bool exists = false)
    {
        var repository = Substitute.For<IFileDetailRepository>();
        repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success<bool, ScrapeError>(exists));
        repository.AddAsync(Arg.Any<FileDetailEntity>(), Arg.Any<CancellationToken>()).Returns(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Instance));

        return repository;
    }

    internal static IFileClassificationCategoriesRepository BuildScrapedTagRepository()
    {
        var repository = Substitute.For<IFileClassificationCategoriesRepository>();
        repository.SaveAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<CancellationToken>()).Returns(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Instance));

        return repository;
    }
}
