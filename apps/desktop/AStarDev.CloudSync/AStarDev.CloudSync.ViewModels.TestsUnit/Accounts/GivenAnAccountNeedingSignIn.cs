using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.Layouts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountNeedingSignIn
{
    [Fact]
    public void when_the_account_needs_sign_in_then_its_status_says_so_and_wins_over_the_sync_status()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), new RecordingAccountCommands());

        (account.NeedsSignIn, account.Status.Text, account.Status.IsUpToDate, account.Status.IsSyncing).ShouldBe((true, TestData.CreateLocalization().GetLocal("Status.SignInRequired"), false, false));
    }

    [Fact]
    public void when_a_status_has_no_sign_in_then_it_is_never_sign_in_required()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateSyncing(40), TestData.CreateLocalization());

        (status.IsSignInRequired, status.IsSyncing).ShouldBe((false, true));
    }

    [Fact]
    public async Task when_sign_in_and_remove_run_then_the_account_commands_are_called_for_that_account()
    {
        var commands = new RecordingAccountCommands();
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), commands);

        await account.SignInCommand.ExecuteAsync(null);
        await account.RemoveCommand.ExecuteAsync(null);

        commands.Calls.ShouldBe(["sign-in:Personal", "remove:Personal"]);
        (account.SignInText, account.RemoveText).ShouldBe((TestData.CreateLocalization().GetLocal("Account.SignIn"), TestData.CreateLocalization().GetLocal("Account.Remove")));
    }

    [Fact]
    public async Task when_a_layout_a_card_needs_sign_in_then_its_action_signs_in_instead_of_syncing()
    {
        var commands = new RecordingAccountCommands();
        var syncActions = new RecordingSyncActions();
        var card = new LayoutAAccountCardViewModel(TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), commands), _ => { }, syncActions, TestData.CreateLocalization());

        await card.ActionCommand.ExecuteAsync(null);

        (card.ActionText, commands.Calls.Single(), syncActions.Calls.Count).ShouldBe((TestData.CreateLocalization().GetLocal("Account.SignIn"), "sign-in:Personal", 0));
    }

    [Fact]
    public async Task when_a_signed_in_card_runs_its_action_then_it_still_syncs()
    {
        var commands = new RecordingAccountCommands();
        var syncActions = new RecordingSyncActions();
        var card = new LayoutAAccountCardViewModel(TestData.CreateAccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), AStarDev.FunctionalParadigm.Option.None<DateTimeOffset>()), commands), _ => { }, syncActions, TestData.CreateLocalization());

        await card.ActionCommand.ExecuteAsync(null);

        (commands.Calls.Count, syncActions.Calls.Count).ShouldBe((0, 1));
    }
}
