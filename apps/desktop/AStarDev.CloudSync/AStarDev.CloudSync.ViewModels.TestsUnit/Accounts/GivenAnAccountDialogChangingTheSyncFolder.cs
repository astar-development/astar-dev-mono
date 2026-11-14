using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountDialogChangingTheSyncFolder
{
    private const string CurrentFolder = "/home/jbarden/CloudSync/OneDrive-Personal";
    private const string NewFolder = "/data/sync";

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
    private readonly FakeAccountActions actions = new();
    private readonly FakeSyncRootReader syncRoots = new();
    private readonly InMemoryAccountStore accounts = new();
    private readonly FakeFolderPicker folderPicker = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_dialog_opens_then_it_shows_the_accounts_current_sync_folder()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        syncRoots.Roots = [Existing(account.Id, CurrentFolder)];

        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        (dialog.IsOpen, dialog.IsShowingSyncFolder, dialog.SyncFolderPathText, dialog.TitleText).ShouldBe((true, true, CurrentFolder, Text("AccountDialog.SyncFolder.Title")));
        await Task.CompletedTask;
    }

    [Fact]
    public void when_the_account_has_no_sync_folder_then_the_dialog_says_so()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());

        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        dialog.SyncFolderPathText.ShouldBe(Text("AccountDialog.SyncFolder.None"));
    }

    [Fact]
    public void when_the_sync_folder_cannot_be_read_then_the_dialog_says_so()
    {
        syncRoots.Fails = true;
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());

        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        dialog.SyncFolderPathText.ShouldBe(Text("AccountDialog.SyncFolder.ReadFailed"));
    }

    [Fact]
    public async Task when_a_new_account_picks_a_folder_then_the_folder_is_changed_without_asking()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        (dialog.IsConfirmingSyncFolderChange, dialog.IsShowingSyncFolder, dialog.SyncFolderPathText, dialog.HasSyncFolderMessage, actions.Calls.Single()).ShouldBe((false, true, NewFolder, false, ("change-sync-root", account.Id)));
    }

    [Fact]
    public async Task when_an_account_that_has_synced_picks_a_folder_then_it_asks_first_and_changes_nothing_yet()
    {
        var (dialog, account) = Create(Synced());
        syncRoots.Roots = [Existing(account.Id, CurrentFolder)];
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        (dialog.IsConfirmingSyncFolderChange, dialog.SyncFolderConfirmText, actions.Calls.Count).ShouldBe((true, TestData.CreateLocalization().GetLocal("AccountDialog.SyncFolder.Confirm.Prompt", "Personal"), 0));
    }

    [Fact]
    public async Task when_the_change_is_confirmed_then_the_folder_is_changed_and_the_new_folder_shows()
    {
        var (dialog, account) = Create(Synced());
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);
        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        await dialog.ConfirmSyncFolderChangeCommand.ExecuteAsync(null);

        (dialog.IsShowingSyncFolder, dialog.SyncFolderPathText, actions.Calls.Single()).ShouldBe((true, NewFolder, ("change-sync-root", account.Id)));
    }

    [Fact]
    public async Task when_the_change_is_not_confirmed_then_the_current_folder_shows_again_and_nothing_changes()
    {
        var (dialog, account) = Create(Synced());
        syncRoots.Roots = [Existing(account.Id, CurrentFolder)];
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);
        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        dialog.CancelSyncFolderChangeCommand.Execute(null);

        (dialog.IsShowingSyncFolder, dialog.SyncFolderPathText, actions.Calls.Count).ShouldBe((true, CurrentFolder, 0));
    }

    [Fact]
    public async Task when_the_user_cancels_the_folder_chooser_then_nothing_changes()
    {
        var (dialog, account) = Create(Synced());
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        (dialog.IsShowingSyncFolder, folderPicker.Calls, actions.Calls.Count).ShouldBe((true, 1, 0));
    }

    [Fact]
    public async Task when_the_folder_clashes_with_another_account_then_the_message_names_that_account_and_the_folder_is_unchanged()
    {
        var other = await AddAccountAsync("Work");
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        syncRoots.Roots = [Existing(account.Id, CurrentFolder)];
        actions.ChangeSyncRoot = (_, _) => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateInsideOf(other)));
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        (dialog.IsShowingSyncFolder, dialog.HasSyncFolderMessage, dialog.SyncFolderMessageText, dialog.SyncFolderPathText).ShouldBe((true, true, TestData.CreateLocalization().GetLocal("AccountDialog.SyncFolder.Failure.InsideOf", "Work"), CurrentFolder));
    }

    [Fact]
    public async Task when_the_clashing_account_is_unknown_then_the_message_says_another_account()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        actions.ChangeSyncRoot = (_, _) => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateSameAs(AccountIdFactory.Create(Guid.NewGuid()))));
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        dialog.SyncFolderMessageText.ShouldBe(TestData.CreateLocalization().GetLocal("AccountDialog.SyncFolder.Failure.SameAs", Text("AccountDialog.SyncFolder.OtherAccount")));
    }

    public static TheoryData<string, string> Rejections => new()
    {
        { "NotAbsolute", "AccountDialog.SyncFolder.Failure.NotAbsolute" },
        { "NotWritable", "AccountDialog.SyncFolder.Failure.NotWritable" },
        { "Unavailable", "AccountDialog.SyncFolder.Failure.Unavailable" },
        { "Reserved", "AccountDialog.SyncFolder.Failure.Reserved" },
        { "SyncStillRunning", "AccountDialog.Failure.SyncStillRunning" },
        { "Store", "AccountDialog.SyncFolder.Failure.Store" },
    };

    [Theory]
    [MemberData(nameof(Rejections))]
    public async Task when_the_change_is_rejected_then_the_localised_reason_shows_inline(string rejectionName, string messageKey)
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        actions.ChangeSyncRoot = (_, _) => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(Rejection(rejectionName)));
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        (dialog.IsShowingSyncFolder, dialog.SyncFolderMessageText).ShouldBe((true, Text(messageKey)));
    }

    [Fact]
    public async Task when_the_change_throws_then_the_store_failure_shows_inline()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        actions.ChangeSyncRoot = (_, _) => throw new InvalidOperationException("boom");
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        dialog.SyncFolderMessageText.ShouldBe(Text("AccountDialog.SyncFolder.Failure.Store"));
    }

    [Fact]
    public async Task when_a_new_pick_succeeds_after_a_failure_then_the_message_is_cleared()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        actions.ChangeSyncRoot = (_, _) => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable()));
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New(NewFolder));
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);
        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);
        actions.ChangeSyncRoot = (_, candidate) => Task.FromResult(SyncRootFactory.Create(candidate));

        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);

        (dialog.HasSyncFolderMessage, dialog.SyncFolderPathText).ShouldBe((false, NewFolder));
    }

    [Fact]
    public void when_the_account_is_syncing_then_the_folder_cannot_be_changed_and_the_reason_shows()
    {
        var (dialog, account) = Create(Syncing());
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        (account.ChangeSyncFolderCommand.CanExecute(null), dialog.ChooseSyncFolderCommand.CanExecute(null), dialog.SyncFolderSyncingText).ShouldBe((false, false, Text("AccountDialog.SyncFolder.Syncing")));
    }

    [Fact]
    public void when_the_account_is_idle_then_the_folder_can_be_changed()
    {
        var (dialog, account) = Create(Synced());
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        (account.ChangeSyncFolderCommand.CanExecute(null), dialog.ChooseSyncFolderCommand.CanExecute(null), dialog.SyncFolderSyncingText).ShouldBe((true, true, string.Empty));
    }

    [Fact]
    public async Task when_the_dialog_is_closed_then_the_wait_ends()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        var shown = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        dialog.CloseCommand.Execute(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void when_another_dialog_is_open_then_asking_again_does_nothing()
    {
        var (dialog, account) = Create(TestData.CreateAccountNeedingSignIn());
        _ = dialog.RemoveAsync(account, CancellationToken);

        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);

        (dialog.IsConfirming, dialog.IsShowingSyncFolder).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_sync_folder_action_runs_on_the_account_then_the_commands_are_asked()
    {
        var commands = new RecordingAccountCommands();
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), commands);

        await account.ChangeSyncFolderCommand.ExecuteAsync(null);

        commands.Calls.ShouldBe(["sync-folder:Personal"]);
    }

    [Fact]
    public void when_the_account_has_a_last_sync_time_then_it_has_synced()
    {
        var never = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), new RecordingAccountCommands());
        var synced = TestData.CreateAccountViewModel(Synced(), new RecordingAccountCommands());

        (never.HasSynced, synced.HasSynced).ShouldBe((false, true));
    }

    private (AccountDialogViewModel Dialog, AccountViewModel Account) Create(CloudAccount cloudAccount)
    {
        var dialog = TestData.CreateAccountDialog(actions, syncRoots, accounts, folderPicker);

        return (dialog, TestData.CreateAccountViewModel(cloudAccount, dialog));
    }

    private AccountSyncRoot Existing(AccountId accountId, string path) =>
        AccountSyncRootFactory.Create(accountId, SyncRootFactory.Create(fileSystem.DirectoryInfo.New(path)).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid.")));

    private async Task<AccountId> AddAccountAsync(string displayName)
    {
        var key = ProviderAccountKeyFactory.Create($"{displayName}-key").Match(created => created, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), displayName, $"{displayName.ToLowerInvariant()}@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

        return (await accounts.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private static CloudAccount Synced() => WithStatus(SyncStatusFactory.CreateUpToDate(), Option.Some(TestData.Now));

    private static CloudAccount Syncing() => WithStatus(SyncStatusFactory.CreateSyncing(40), Option.Some(TestData.Now));

    private static CloudAccount WithStatus(SyncStatus status, Option<DateTimeOffset> lastSync) =>
        CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(AccountIdFactory.Create(Guid.Parse("6f7e8d9c-0b1a-4c3d-9e8f-7a6b5c4d3e2f")), "Personal", "personal@example.com", CloudProviderFactory.CreateOneDrive()), StorageUsageFactory.Create(1, 2), 4, 1_204, status, lastSync, AccountSignInStateFactory.CreateSignedIn());

    private static SyncRootRejection Rejection(string name) => name switch
    {
        "NotAbsolute" => SyncRootRejectionFactory.CreateNotAbsolute(),
        "NotWritable" => SyncRootRejectionFactory.CreateNotWritable(),
        "Unavailable" => SyncRootRejectionFactory.CreateUnavailable(),
        "Reserved" => SyncRootRejectionFactory.CreateReserved(),
        "SyncStillRunning" => SyncRootRejectionFactory.CreateSyncStillRunning(),
        _ => SyncRootRejectionFactory.CreateStore(new ErrorResponse("failed")),
    };

    private static string Text(string key) => TestData.CreateLocalization().GetLocal(key);
}
