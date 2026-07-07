using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Models;

public sealed class GivenThePageInfoFactory
{
    [Fact]
    public void when_creating_a_page_info_then_the_page_count_is_preserved() =>
        PageInfoFactory.Create(5, 120, "sub-directory").PageCount.ShouldBe(5);

    [Fact]
    public void when_creating_a_page_info_then_the_image_count_is_preserved() =>
        PageInfoFactory.Create(5, 120, "sub-directory").ImageCount.ShouldBe(120);

    [Fact]
    public void when_creating_a_page_info_then_the_sub_directory_name_is_preserved() =>
        PageInfoFactory.Create(5, 120, "sub-directory").SubDirectoryName.ShouldBe("sub-directory");
}
