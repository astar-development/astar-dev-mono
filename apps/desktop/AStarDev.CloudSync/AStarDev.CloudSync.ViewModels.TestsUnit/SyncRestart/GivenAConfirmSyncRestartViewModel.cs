using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.SyncRestart;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.SyncRestart;

public class GivenAConfirmSyncRestartViewModel
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static ConfirmSyncRestartViewModel Create() => new(TestData.CreateLocalization());

    [Fact]
    public void when_nothing_was_asked_then_the_dialog_is_closed()
    {
        Create().IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void when_asked_then_the_dialog_opens_and_waits()
    {
        var dialog = Create();

        var answer = dialog.AskAsync(Personal, CancellationToken);

        dialog.IsOpen.ShouldBeTrue();
        answer.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_user_keeps_the_current_sync_then_the_answer_is_keep_and_the_dialog_closes()
    {
        var dialog = Create();
        var answer = dialog.AskAsync(Personal, CancellationToken);

        dialog.KeepCommand.Execute(null);

        (await answer).ShouldBe(SyncRestartChoiceFactory.CreateKeepCurrent());
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_user_chooses_to_cancel_and_start_again_then_the_answer_is_cancel_and_restart_and_the_dialog_closes()
    {
        var dialog = Create();
        var answer = dialog.AskAsync(Personal, CancellationToken);

        dialog.CancelAndRestartCommand.Execute(null);

        (await answer).ShouldBe(SyncRestartChoiceFactory.CreateCancelAndRestart());
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_asked_again_while_open_then_both_callers_get_the_one_answer()
    {
        var dialog = Create();
        var first = dialog.AskAsync(Personal, CancellationToken);
        var second = dialog.AskAsync(Personal, CancellationToken);

        dialog.CancelAndRestartCommand.Execute(null);

        (await first).ShouldBe(SyncRestartChoiceFactory.CreateCancelAndRestart());
        (await second).ShouldBe(SyncRestartChoiceFactory.CreateCancelAndRestart());
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_dialog_closes_and_the_answer_is_keep()
    {
        var dialog = Create();
        using var source = new CancellationTokenSource();
        var answer = dialog.AskAsync(Personal, source.Token);

        await source.CancelAsync();

        (await answer).ShouldBe(SyncRestartChoiceFactory.CreateKeepCurrent());
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void when_the_dialog_is_closed_then_an_answer_command_does_nothing()
    {
        var dialog = Create();

        dialog.KeepCommand.Execute(null);

        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_asked_after_an_answer_then_a_new_question_opens()
    {
        var dialog = Create();
        var first = dialog.AskAsync(Personal, CancellationToken);
        dialog.KeepCommand.Execute(null);
        _ = await first;

        var second = dialog.AskAsync(Personal, CancellationToken);

        dialog.IsOpen.ShouldBeTrue();
        second.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void when_the_texts_are_read_then_they_are_localised()
    {
        var dialog = Create();

        (dialog.TitleText, dialog.CancelAndRestartText, dialog.KeepText).ShouldBe(("A sync is already running", "Cancel the current sync and start again", "Keep the current sync"));
        dialog.MessageText.ShouldBe("This account is already syncing. Do you want to cancel the current sync and start again?");
    }
}
