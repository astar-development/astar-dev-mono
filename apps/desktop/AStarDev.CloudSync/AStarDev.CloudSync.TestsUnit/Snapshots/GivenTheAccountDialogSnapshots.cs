using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheAccountDialogSnapshots
{
    [AvaloniaTheory]
    [InlineData("confirm", "dark")]
    [InlineData("confirm", "light")]
    [InlineData("failed", "dark")]
    [InlineData("failed", "light")]
    public async Task when_rendered_then_the_account_dialog_matches_the_baseline(string step, string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var actions = new FakeAccountActions { Remove = _ => Task.FromResult(Result.Failure<Unit, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateSyncStillRunning())) };
        var dialog = TestData.CreateAccountDialog(actions);
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), dialog);
        _ = dialog.RemoveAsync(account, TestContext.Current.CancellationToken);
        if (step == "failed")
            await dialog.ConfirmRemoveCommand.ExecuteAsync(null);
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncBackgroundBrush"),
                Child = new AccountDialogView { DataContext = dialog }
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-account-dialog-{step}-{theme}", 520, 280);
    }

    [AvaloniaTheory]
    [InlineData("sync-folder", "dark")]
    [InlineData("sync-folder", "light")]
    [InlineData("sync-folder-confirm", "dark")]
    [InlineData("sync-folder-confirm", "light")]
    [InlineData("sync-folder-failed", "dark")]
    [InlineData("sync-folder-failed", "light")]
    public async Task when_rendered_with_a_sync_folder_then_the_account_dialog_matches_the_baseline(string step, string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var fileSystem = new Testably.Abstractions.Testing.MockFileSystem(options => options.SimulatingOperatingSystem(Testably.Abstractions.Testing.SimulationMode.Linux));
        var account = TestData.CreateAccountNeedingSignIn();
        var syncedAccount = CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(account.Id, account.DisplayName, account.Email, account.Provider), account.Storage, account.FolderCount, account.FileCount, SyncStatusFactory.CreateUpToDate(), Option.Some(TestData.Now), AccountSignInStateFactory.CreateSignedIn());
        var syncRoots = new FakeSyncRootReader
        {
            Roots = [AccountSyncRootFactory.Create(account.Id, SyncRootFactory.Create(fileSystem.DirectoryInfo.New("/home/jbarden/CloudSync/OneDrive-Personal")).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid.")))],
        };
        var actions = new FakeAccountActions { ChangeSyncRoot = (_, _) => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateNotWritable())) };
        var picker = new FakeFolderPicker { Choice = Option.Some(fileSystem.DirectoryInfo.New("/data/sync")) };
        var dialog = TestData.CreateAccountDialog(actions, syncRoots, new InMemoryAccountStore(), picker);
        var accountViewModel = TestData.CreateAccountViewModel(syncedAccount, dialog);
        _ = dialog.ChangeSyncFolderAsync(accountViewModel, TestContext.Current.CancellationToken);
        if (step != "sync-folder")
            await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);
        if (step == "sync-folder-failed")
            await dialog.ConfirmSyncFolderChangeCommand.ExecuteAsync(null);
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncBackgroundBrush"),
                Child = new AccountDialogView { DataContext = dialog }
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-account-dialog-{step}-{theme}", 520, 300);
    }
}
