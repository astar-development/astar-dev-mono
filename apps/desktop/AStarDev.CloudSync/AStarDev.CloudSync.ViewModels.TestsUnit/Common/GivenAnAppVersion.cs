using AStarDev.CloudSync.ViewModels;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Common;

public class GivenAnAppVersion
{
    [Theory]
    [InlineData("1.71.1", "1.71.1")]
    [InlineData("v1.71.1", "1.71.1")]
    [InlineData("V1.71.1", "1.71.1")]
    [InlineData("1.71.1+abc123", "1.71.1")]
    [InlineData("v1.71.1-beta.1+sha.abc", "1.71.1-beta.1")]
    [InlineData("  1.2.3  ", "1.2.3")]
    public void when_a_version_is_given_then_the_leading_v_and_build_metadata_are_dropped(string raw, string expected) => AppVersionFactory.Create(raw).Text.ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("+abc")]
    [InlineData("v")]
    public void when_no_usable_version_is_given_then_the_local_fallback_is_used(string? raw) => AppVersionFactory.Create(raw).Text.ShouldBe("0.1.0");
}
