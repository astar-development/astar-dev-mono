using System.Globalization;
using Avalonia.Platform.Storage;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

public sealed class GivenASettingsViewModelBrowseForAccountFolder
{
    private const string KnownAccountId = "account-1";
    private const string UnknownAccountId = "unknown-id";
    private const string PickedPath = "/home/user/OneDrive";

    private static IFolderPickerService BuildFolderPickerService(string? pathToReturn = PickedPath)
    {
        var service = Substitute.For<IFolderPickerService>();
        service.PickFolderAsync(Arg.Any<IStorageProvider>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(pathToReturn));

        return service;
    }

    private static ISettingsService BuildSettingsService()
    {
        var service = Substitute.For<ISettingsService>();
        service.Current.Returns(new AppSettings());
        service.SaveAsync().Returns(Task.CompletedTask);

        return service;
    }

    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => x.ArgAt<string>(0));
        loc.AvailableCultures.Returns([CultureInfo.GetCultureInfo("en-GB")]);

        return loc;
    }

    private static SettingsViewModel BuildSut(IFolderPickerService? folderPickerService = null)
        => new(BuildSettingsService(), Substitute.For<IThemeService>(), Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), BuildLocalizationService(), folderPickerService ?? BuildFolderPickerService());

    private static OneDriveAccount BuildAccount(string accountId) => new()
    {
        Id = new AccountId(accountId),
        Profile = AccountProfileFactory.Create("Test User", "test@example.com")
    };

    [Fact]
    public async Task when_called_with_unknown_account_id_then_folder_picker_is_not_called()
    {
        var pickerService = BuildFolderPickerService();
        var sut = BuildSut(pickerService);

        await sut.BrowseForAccountFolderAsync(UnknownAccountId, Substitute.For<IStorageProvider>(), TestContext.Current.CancellationToken);

        await pickerService.DidNotReceive().PickFolderAsync(Arg.Any<IStorageProvider>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_called_with_known_account_and_picker_returns_path_then_local_sync_path_is_updated()
    {
        var sut = BuildSut(BuildFolderPickerService(PickedPath));
        sut.AddAccount(BuildAccount(KnownAccountId));

        await sut.BrowseForAccountFolderAsync(KnownAccountId, Substitute.For<IStorageProvider>(), TestContext.Current.CancellationToken);

        sut.AccountSettings.Single(a => a.AccountId == KnownAccountId).LocalSyncPath.ShouldBe(PickedPath);
    }

    [Fact]
    public async Task when_called_with_known_account_and_picker_returns_null_then_local_sync_path_is_not_changed()
    {
        var sut = BuildSut(BuildFolderPickerService(null));
        sut.AddAccount(BuildAccount(KnownAccountId));
        string originalPath = sut.AccountSettings.Single(a => a.AccountId == KnownAccountId).LocalSyncPath;

        await sut.BrowseForAccountFolderAsync(KnownAccountId, Substitute.For<IStorageProvider>(), TestContext.Current.CancellationToken);

        sut.AccountSettings.Single(a => a.AccountId == KnownAccountId).LocalSyncPath.ShouldBe(originalPath);
    }
}
