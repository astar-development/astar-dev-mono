using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenTheClassificationMatcher
{
    private static FileDetailEntity BuildFileDetail(string fileName)
        => new() { FileName = new FileName(fileName), DirectoryName = new DirectoryName("/tmp"), };

    [Fact]
    public void when_there_are_no_searchable_classifications_and_no_category_classification_then_the_result_is_empty()
    {
        var pageData = new PageClassificationData([], null, []);

        var matched = ClassificationMatcher.Match(pageData, BuildFileDetail("test.jpg"));

        matched.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_searchable_classification_keyword_matches_the_file_name_then_it_is_included()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, };
        var pageData = new PageClassificationData([(category, ["animals",])], null, []);

        var matched = ClassificationMatcher.Match(pageData, BuildFileDetail("animals-photo.jpg"));

        matched.ShouldContain(category);
    }

    [Fact]
    public void when_a_searchable_classification_keyword_does_not_match_the_file_name_then_it_is_not_included()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, };
        var pageData = new PageClassificationData([(category, ["animals",])], null, []);

        var matched = ClassificationMatcher.Match(pageData, BuildFileDetail("cars-photo.jpg"));

        matched.ShouldNotContain(category);
    }

    [Fact]
    public void when_a_category_classification_is_supplied_then_it_is_included()
    {
        var categoryClassification = new FileClassificationCategoryEntity { Name = "Cars", Level = 3, };
        var pageData = new PageClassificationData([], categoryClassification, []);

        var matched = ClassificationMatcher.Match(pageData, BuildFileDetail("test.jpg"));

        matched.ShouldContain(categoryClassification);
    }

    [Fact]
    public void when_no_category_classification_is_supplied_then_none_is_included_from_it()
    {
        var pageData = new PageClassificationData([], null, []);

        var matched = ClassificationMatcher.Match(pageData, BuildFileDetail("test.jpg"));

        matched.ShouldBeEmpty();
    }

    [Fact]
    public void when_both_a_filename_match_and_a_category_classification_are_present_then_both_are_included()
    {
        var fileNameCategory = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, };
        var categoryClassification = new FileClassificationCategoryEntity { Name = "Cars", Level = 3, };
        var pageData = new PageClassificationData([(fileNameCategory, ["animals",])], categoryClassification, []);

        var matched = ClassificationMatcher.Match(pageData, BuildFileDetail("animals-photo.jpg"));

        matched.Count.ShouldBe(2);
    }

    [Fact]
    public void when_multiple_searchable_classifications_match_the_file_name_then_all_are_included()
    {
        var animals = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, };
        var outdoors = new FileClassificationCategoryEntity { Name = "Outdoors", Level = 3, };
        var pageData = new PageClassificationData([(animals, ["animals",]), (outdoors, ["outdoors",])], null, []);

        var matched = ClassificationMatcher.Match(pageData, BuildFileDetail("animals-outdoors-photo.jpg"));

        matched.Count.ShouldBe(2);
    }
}
