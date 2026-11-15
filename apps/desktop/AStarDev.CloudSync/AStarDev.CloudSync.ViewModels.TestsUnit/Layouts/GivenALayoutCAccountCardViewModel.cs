using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Layouts;

public class GivenALayoutCAccountCardViewModel
{
    [Fact]
    public async Task when_the_pause_command_runs_then_the_account_is_paused_or_resumed()
    {
        var syncActions = new RecordingSyncActions();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());
        var card = new LayoutCAccountCardViewModel(account, _ => { }, syncActions, TestData.CreateLocalization());

        await card.PauseCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe([$"PauseAsync:{account.Id}"]);
    }

    [Fact]
    public void when_the_account_needs_sign_in_then_the_pause_command_cannot_run()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), new RecordingAccountCommands());
        var card = new LayoutCAccountCardViewModel(account, _ => { }, new RecordingSyncActions(), TestData.CreateLocalization());

        card.PauseCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void when_the_card_is_selected_through_its_command_then_the_card_is_passed_to_the_select_callback()
    {
        LayoutCAccountCardViewModel? passed = null;
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());
        var card = new LayoutCAccountCardViewModel(account, selected => passed = selected, new RecordingSyncActions(), TestData.CreateLocalization());

        card.SelectCommand.Execute(null);

        passed.ShouldBeSameAs(card);
    }

    [Fact]
    public async Task when_the_action_command_runs_while_the_account_is_not_syncing_then_the_account_syncs_now()
    {
        var syncActions = new RecordingSyncActions();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());
        var card = new LayoutCAccountCardViewModel(account, _ => { }, syncActions, TestData.CreateLocalization());

        await card.ActionCommand.ExecuteAsync(null);

        card.ActionText.ShouldBe("Sync now");
        syncActions.Calls.ShouldBe([$"SyncNowAsync:{account.Id}"]);
    }

    [Fact]
    public async Task when_the_action_command_runs_while_the_account_is_syncing_then_the_sync_is_cancelled()
    {
        var syncActions = new RecordingSyncActions();
        var account = new AccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateSyncing(25), Option.None<DateTimeOffset>()), [], TestData.CreateLocalization(), TestData.CreateTime());
        var card = new LayoutCAccountCardViewModel(account, _ => { }, syncActions, TestData.CreateLocalization());

        await card.ActionCommand.ExecuteAsync(null);

        card.ActionText.ShouldBe("Cancel");
        syncActions.Calls.ShouldBe([$"CancelSyncAsync:{account.Id}"]);
    }

    [Fact]
    public void when_the_account_needs_sign_in_then_the_action_command_cannot_run()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), new RecordingAccountCommands());
        var card = new LayoutCAccountCardViewModel(account, _ => { }, new RecordingSyncActions(), TestData.CreateLocalization());

        card.ActionCommand.CanExecute(null).ShouldBeFalse();
    }
}
