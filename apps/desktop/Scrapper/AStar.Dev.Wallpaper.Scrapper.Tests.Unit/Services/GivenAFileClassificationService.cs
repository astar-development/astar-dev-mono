using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

// TODO(#697): FileClassificationService is stubbed as a no-op pending the rewrite against the
// FileClassificationCategoryEntity/FileClassificationKeywordEntity hierarchy - see the TODO on the
// service itself. These tests cover the stub's current, intentional behavior only.
public sealed class GivenAFileClassificationService
{
    private readonly FileClassificationService sut = new();

    [Fact]
    public async Task when_loading_page_classification_data_then_searchable_classifications_are_empty()
    {
        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.SearchableClassifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_loading_page_classification_data_then_category_classification_is_null()
    {
        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.CategoryClassification.ShouldBeNull();
    }

    [Fact]
    public async Task when_loading_page_classification_data_then_included_tags_are_empty()
    {
        var result = await sut.LoadPageClassificationDataAsync("any-category", TestContext.Current.CancellationToken);

        result.IncludedTags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_classifying_a_file_then_it_completes_without_error()
    {
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("test.jpg"),
            DirectoryName = new DirectoryName("/tmp")
        };
        var pageData = new PageClassificationData([], null, []);

        await Should.NotThrowAsync(() => sut.ClassifyAsync(fileDetail, pageData, [], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_exporting_classifications_then_an_empty_list_is_returned()
    {
        var result = await sut.ExportClassificationsAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_importing_classifications_then_the_result_reports_failure()
    {
        var classification = new FileClassificationCategoryEntity { Name = "Animals" };

        var result = await sut.ImportClassificationsAsync([classification], TestContext.Current.CancellationToken);

        dynamic dynamicResult = result;
        ((bool)dynamicResult.Success).ShouldBeFalse();
        ((int)dynamicResult.Count).ShouldBe(0);
    }
}
