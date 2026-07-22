using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using Velopack;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenUpdateService
{
    private static (UpdateService Sut, IVelopackUpdateService UpdateService) CreateSut()
    {
        var updateService = Substitute.For<IVelopackUpdateService>();
        var logger = Substitute.For<ILogger<UpdateService>>();

        return (new UpdateService(updateService, logger), updateService);
    }

    [Fact]
    public async Task when_no_update_is_available_then_the_check_completes_without_downloading()
    {
        var (sut, updateService) = CreateSut();
        updateService.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns((UpdateInfo?)null);

        await Should.NotThrowAsync(() => sut.CheckForUpdatesAsync(null!));

        await updateService.DidNotReceive().DownloadUpdatesAsync(Arg.Any<UpdateInfo>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_update_check_throws_then_the_failure_is_swallowed()
    {
        var (sut, updateService) = CreateSut();
        updateService.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("offline"));

        await Should.NotThrowAsync(() => sut.CheckForUpdatesAsync(null!));

        updateService.DidNotReceive().ApplyUpdatesAndRestart(Arg.Any<UpdateInfo>());
    }
}
