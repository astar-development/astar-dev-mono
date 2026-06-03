using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

public sealed class GivenASettingsViewModelWithLocalSyncPathApply
{
    private const string KnownAccountId = "account-1";
    private const string UnknownAccountId = "unknown-id";
    private const string NewPath = "/home/user/OneDrive";

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

    private static SettingsViewModel BuildSut()
        => new(BuildSettingsService(), Substitute.For<IThemeService>(), Substitute.For<ISyncScheduler>(), Substitute.For<IAccountRepository>(), BuildLocalizationService(), Substitute.For<IFolderPickerService>());

    private static OneDriveAccount BuildAccount(string accountId) => new()
    {
        Id = new AccountId(accountId),
        Profile = AccountProfileFactory.Create("Test User", "test@example.com")
    };

    [Fact]
    public void when_called_with_known_account_id_then_returns_true()
    {
        var sut = BuildSut();
        sut.AddAccount(BuildAccount(KnownAccountId));

        var result = sut.TryApplyLocalSyncPath(KnownAccountId, NewPath);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_called_with_known_account_id_then_local_sync_path_is_updated()
    {
        var sut = BuildSut();
        sut.AddAccount(BuildAccount(KnownAccountId));

        sut.TryApplyLocalSyncPath(KnownAccountId, NewPath);

        sut.AccountSettings.Single(a => a.AccountId == KnownAccountId).LocalSyncPath.ShouldBe(NewPath);
    }

    [Fact]
    public void when_called_with_unknown_account_id_then_returns_false()
    {
        var sut = BuildSut();

        var result = sut.TryApplyLocalSyncPath(UnknownAccountId, NewPath);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_called_with_unknown_account_id_then_account_settings_are_unchanged()
    {
        var sut = BuildSut();
        sut.AddAccount(BuildAccount(KnownAccountId));

        sut.TryApplyLocalSyncPath(UnknownAccountId, NewPath);

        sut.AccountSettings.ShouldNotContain(a => a.LocalSyncPath == NewPath);
    }
}
