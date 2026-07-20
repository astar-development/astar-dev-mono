using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Updates;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Updates;
using Microsoft.Extensions.Logging;
using Velopack;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Updates;

public sealed class GivenAnUpdateNotificationService
{
    private static UpdateInfo CreateUpdateInfo(string version = "1.2.3")
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse(version), NotesMarkdown = "notes" };

        return new UpdateInfo(asset, false, null, []);
    }

    private static UpdateAvailableViewModel CreateViewModel(UpdateInfo updateInfo)
    {
        var updateCheckService = Substitute.For<IUpdateCheckService>();
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());
        var logger = Substitute.For<ILogger<UpdateAvailableViewModel>>();

        return new UpdateAvailableViewModel(updateInfo, updateCheckService, localization, logger);
    }

    private static (UpdateNotificationService Sut, IUpdateCheckService UpdateCheckService, IUpdateAvailableViewModelFactory ViewModelFactory, IUpdateAvailableDialogService DialogService) CreateSut()
    {
        var updateCheckService = Substitute.For<IUpdateCheckService>();
        var viewModelFactory = Substitute.For<IUpdateAvailableViewModelFactory>();
        var dialogService = Substitute.For<IUpdateAvailableDialogService>();
        var sut = new UpdateNotificationService(updateCheckService, viewModelFactory, dialogService);

        return (sut, updateCheckService, viewModelFactory, dialogService);
    }

    [Fact]
    public async Task when_no_update_is_available_then_dialog_is_never_shown()
    {
        var (sut, updateCheckService, _, dialogService) = CreateSut();
        updateCheckService.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns((UpdateInfo?)null);

        await sut.CheckAndNotifyAsync(TestContext.Current.CancellationToken);

        await dialogService.DidNotReceive().ShowAsync(Arg.Any<UpdateAvailableViewModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_an_update_is_available_then_view_model_is_created_from_the_update_info()
    {
        var (sut, updateCheckService, viewModelFactory, _) = CreateSut();
        var updateInfo = CreateUpdateInfo();
        var viewModel = CreateViewModel(updateInfo);
        updateCheckService.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns(updateInfo);
        viewModelFactory.Create(updateInfo).Returns(viewModel);

        await sut.CheckAndNotifyAsync(TestContext.Current.CancellationToken);

        viewModelFactory.Received(1).Create(updateInfo);
    }

    [Fact]
    public async Task when_an_update_is_available_then_dialog_is_shown_with_the_created_view_model()
    {
        var (sut, updateCheckService, viewModelFactory, dialogService) = CreateSut();
        var updateInfo = CreateUpdateInfo();
        var viewModel = CreateViewModel(updateInfo);
        updateCheckService.CheckForUpdatesAsync(Arg.Any<CancellationToken>()).Returns(updateInfo);
        viewModelFactory.Create(updateInfo).Returns(viewModel);

        await sut.CheckAndNotifyAsync(TestContext.Current.CancellationToken);

        await dialogService.Received(1).ShowAsync(viewModel, Arg.Any<CancellationToken>());
    }
}
