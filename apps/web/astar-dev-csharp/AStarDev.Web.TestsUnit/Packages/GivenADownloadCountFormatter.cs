using AStarDev.Web.Packages;

namespace AStarDev.Web.TestsUnit.Packages;

public class GivenADownloadCountFormatter
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1_000, "1.0K")]
    [InlineData(1_500, "1.5K")]
    [InlineData(999_999, "1000.0K")]
    [InlineData(1_000_000, "1.0M")]
    [InlineData(2_340_000, "2.3M")]
    public void when_formatting_a_download_count_then_the_correct_string_is_returned(long totalDownloads, string expected)
    {
        DownloadCountFormatter.Format(totalDownloads).ShouldBe(expected);
    }
}
