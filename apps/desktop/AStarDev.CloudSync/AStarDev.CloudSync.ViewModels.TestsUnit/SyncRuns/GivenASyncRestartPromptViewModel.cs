using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.SyncRuns;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.SyncRuns;

public class GivenASyncRestartPromptViewModel
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly ImmediateUiDispatcher dispatcher = new();

    private SyncRestartPromptViewModel CreatePrompt() => new(dispatcher, TestData.CreateLocalization());

    [Fact]
    public void when_created_then_no_question_is_showing_and_the_texts_are_localised()
    {
        var prompt = CreatePrompt();

        prompt.IsAsking.ShouldBeFalse();
        (prompt.TitleText, prompt.MessageText, prompt.RestartText, prompt.KeepText).ShouldBe(("A sync is already running", "Do you want to cancel the current sync and start again?", "Cancel the current sync and start again", "Keep the current sync"));
    }

    [Fact]
    public async Task when_the_question_is_asked_then_it_shows_through_the_ui_dispatcher_until_answered()
    {
        var prompt = CreatePrompt();

        var answer = prompt.AskAsync(Account, TestContext.Current.CancellationToken);

        (prompt.IsAsking, answer.IsCompleted, dispatcher.Invocations > 0).ShouldBe((true, false, true));
        prompt.KeepCommand.Execute(null);
        _ = await answer;
    }

    [Fact]
    public async Task when_the_user_chooses_to_restart_then_the_answer_is_cancel_and_restart_and_the_question_closes()
    {
        var prompt = CreatePrompt();

        var answer = prompt.AskAsync(Account, TestContext.Current.CancellationToken);
        prompt.RestartCommand.Execute(null);

        (await answer).ShouldBe(SyncRestartChoiceFactory.CreateCancelAndRestart());
        prompt.IsAsking.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_user_keeps_the_current_sync_then_the_answer_is_keep_and_the_question_closes()
    {
        var prompt = CreatePrompt();

        var answer = prompt.AskAsync(Account, TestContext.Current.CancellationToken);
        prompt.KeepCommand.Execute(null);

        (await answer).ShouldBe(SyncRestartChoiceFactory.CreateKeepCurrent());
        prompt.IsAsking.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_request_is_cancelled_then_the_answer_is_keep_and_the_question_closes()
    {
        var prompt = CreatePrompt();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var answer = prompt.AskAsync(Account, cancellation.Token);
        await cancellation.CancelAsync();

        (await answer).ShouldBe(SyncRestartChoiceFactory.CreateKeepCurrent());
        prompt.IsAsking.ShouldBeFalse();
    }

    [Fact]
    public void when_a_button_is_pressed_with_no_question_showing_then_nothing_happens()
    {
        var prompt = CreatePrompt();

        prompt.RestartCommand.Execute(null);

        prompt.IsAsking.ShouldBeFalse();
    }
}
