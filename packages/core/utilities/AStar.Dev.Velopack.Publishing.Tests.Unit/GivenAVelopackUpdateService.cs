using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Velopack.Publishing.Tests.Unit;

public sealed class GivenAVelopackUpdateService
{
    private static VelopackUpdateService BuildSut(string channelPrefix)
    {
        var settings = Options.Create(new VelopackUpdateSettings
        {
            GithubRepositoryUrl = new Uri("https://github.com/astar-development/astar-dev-mono"),
            ChannelPrefix       = channelPrefix
        });

        return new VelopackUpdateService(settings, NullLogger<VelopackUpdateService>.Instance);
    }

    [Fact]
    public void when_constructed_then_channel_combines_prefix_and_current_platform()
    {
        var sut = BuildSut("onedrive-sync");

        var expectedSuffix = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";

        sut.Channel.ShouldBe($"onedrive-sync-{expectedSuffix}");
    }

    [Fact]
    public void when_constructed_with_a_different_prefix_then_channel_reflects_it()
    {
        var sut = BuildSut("clock");

        var expectedSuffix = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";

        sut.Channel.ShouldBe($"clock-{expectedSuffix}");
    }
}
