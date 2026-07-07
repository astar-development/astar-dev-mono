using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Repositories;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Models;

public sealed class GivenTheImagePageOutcomeFactory
{
    private static readonly TagData[] RawTags = [new("Sports Car", "Vehicles > Cars & Motorcycles"),];

    [Fact]
    public void when_creating_a_scraped_image_then_the_image_url_is_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", ["tag",], RawTags).ImageUrl.ShouldBe("https://example.test/image.jpg");

    [Fact]
    public void when_creating_a_scraped_image_then_the_directory_segments_are_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", ["tag",], RawTags).DirectorySegments.ShouldBe(["dir",]);

    [Fact]
    public void when_creating_a_scraped_image_then_the_file_prefix_is_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", ["tag",], RawTags).FilePrefix.ShouldBe("prefix");

    [Fact]
    public void when_creating_a_scraped_image_then_the_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", ["tag",], RawTags).Tags.ShouldBe(["tag",]);

    [Fact]
    public void when_creating_a_scraped_image_then_the_raw_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateScrapedImage("https://example.test/image.jpg", ["dir",], "prefix", ["tag",], RawTags).RawTags.ShouldBe(RawTags);

    [Fact]
    public void when_creating_a_skipped_image_then_the_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateSkippedImage(["ignored-tag",], RawTags).Tags.ShouldBe(["ignored-tag",]);

    [Fact]
    public void when_creating_a_skipped_image_then_the_raw_tags_are_preserved() =>
        ImagePageOutcomeFactory.CreateSkippedImage(["ignored-tag",], RawTags).RawTags.ShouldBe(RawTags);
}
