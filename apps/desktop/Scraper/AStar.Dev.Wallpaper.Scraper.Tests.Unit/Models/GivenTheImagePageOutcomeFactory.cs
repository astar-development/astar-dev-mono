using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Models;

public sealed class GivenTheImagePageOutcomeFactory
{
    private static readonly TagData[] RawTags = [new("Sports Car", "Vehicles > Cars & Motorcycles"),];

    [Fact]
    public void when_creating_a_scraped_image_then_the_image_url_is_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", [new TagData("tag","tag")], RawTags).ImageUrl.ShouldBe("https://example.test/image.jpg");

    [Fact]
    public void when_creating_a_scraped_image_then_the_directory_segments_are_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", [new TagData("tag","tag")], RawTags).DirectorySegments.ShouldBe(["dir",]);

    [Fact]
    public void when_creating_a_scraped_image_then_the_file_prefix_is_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", [new TagData("tag","tag")], RawTags).FilePrefix.ShouldBe("prefix");

    [Fact]
    public void when_creating_a_scraped_image_then_the_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", [new TagData("tag","tag")], RawTags).Tags.ShouldBe([new TagData("tag","tag")]);

    [Fact]
    public void when_creating_a_scraped_image_then_the_raw_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", [new TagData("tag","tag")], RawTags).RawTags.ShouldBe(RawTags);

    [Fact]
    public void when_creating_a_skipped_image_then_the_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateSkippedImage([new TagData("ignored-tag","ignored-tag")], RawTags).Tags.ShouldBe([new TagData("ignored-tag","ignored-tag")]);

    [Fact]
    public void when_creating_a_skipped_image_then_the_raw_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateSkippedImage([new TagData("ignored-tag","ignored-tag")], RawTags).RawTags.ShouldBe(RawTags);
}
