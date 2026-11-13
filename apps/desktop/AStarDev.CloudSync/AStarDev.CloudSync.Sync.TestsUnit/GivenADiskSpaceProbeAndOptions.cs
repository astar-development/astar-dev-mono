using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenADiskSpaceProbeAndOptions
{
    [Fact]
    public void when_the_volume_is_known_then_its_free_space_is_returned()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        _ = fileSystem.WithDrive("/", drive => drive.SetTotalSize(10_000));
        using var provider = new ServiceCollection().AddCloudSync().BuildServiceProvider();

        var free = provider.GetRequiredService<IDiskSpaceProbe>().GetAvailableBytes(fileSystem.DirectoryInfo.New("/sync/root"));

        free.Match(bytes => bytes, () => -1).ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void when_the_volume_cannot_be_read_then_no_free_space_is_returned()
    {
        using var provider = new ServiceCollection().AddCloudSync().BuildServiceProvider();

        var free = provider.GetRequiredService<IDiskSpaceProbe>().GetAvailableBytes(null!);

        free.Match(_ => true, () => false).ShouldBeFalse();
    }

    [Fact]
    public void when_options_are_created_with_defaults_then_the_concurrency_is_eight_and_the_margin_is_one_hundred_megabytes()
    {
        var options = DownloadPassOptionsFactory.Create();

        (options.MaxConcurrentDownloads, options.FreeSpaceMarginBytes, options.BaselineChunkSize).ShouldBe((8, 104_857_600L, 100));
    }

    [Fact]
    public void when_options_are_below_their_minimum_then_they_are_raised_to_it()
    {
        var options = DownloadPassOptionsFactory.Create(0, -5, 0);

        (options.MaxConcurrentDownloads, options.FreeSpaceMarginBytes, options.BaselineChunkSize).ShouldBe((1, 0L, 1));
    }
}
