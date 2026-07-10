using AStar.Dev.Wallpaper.Scraper.Repositories;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Repositories;

public sealed class GivenTagData
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("People > model", true)]
    [InlineData("People > Model", true)]
    [InlineData("People > actress", false)]
    [InlineData("People > Actress", false)]
    public void with_the_supplied_category_then_isInternet_should_be_the_expected_value_ignoring_case(string? category, bool expectedIsInternetValue)
    {
        var sut = new TagData("Not relevant to this test", category);

        sut.IsInternet.ShouldBe(expectedIsInternetValue);
    }
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("People > model", false)]
    [InlineData("People > Model", false)]
    [InlineData("People > porn", true)]
    [InlineData("People > Porn", true)]
    [InlineData("People > actress", true)]
    [InlineData("People > Actress", true)]
    [InlineData("People > celebrity", true)]
    [InlineData("People > Celebrity", true)]
    [InlineData("People > actor", true)]
    [InlineData("People > Actor", true)]
    [InlineData("People > singer", true)]
    [InlineData("People > Singer", true)]
    public void with_the_supplied_category_then_isFamous_should_be_the_expected_value_ignoring_case(string? category, bool expectedIsFamousValue)
    {
        var sut = new TagData("Not relevant to this test", category);

        sut.IsFamous.ShouldBe(expectedIsFamousValue);
    }
}
