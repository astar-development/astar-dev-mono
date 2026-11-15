using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Tray;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

public class GivenAConfirmQuitViewModel
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static ConfirmQuitViewModel Create() => new(TestData.CreateLocalization());

    [Fact]
    public void when_nothing_was_asked_then_the_dialog_is_closed()
    {
        Create().IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void when_asked_then_the_dialog_opens_and_waits()
    {
        var dialog = Create();

        var answer = dialog.AskAsync(CancellationToken);

        dialog.IsOpen.ShouldBeTrue();
        answer.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_user_chooses_to_quit_then_the_answer_is_quit_anyway_and_the_dialog_closes()
    {
        var dialog = Create();
        var answer = dialog.AskAsync(CancellationToken);

        dialog.QuitCommand.Execute(null);

        (await answer).ShouldBe(QuitChoiceFactory.CreateQuitAnyway());
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_user_keeps_syncing_then_the_answer_is_stay_open_and_the_dialog_closes()
    {
        var dialog = Create();
        var answer = dialog.AskAsync(CancellationToken);

        dialog.StayCommand.Execute(null);

        (await answer).ShouldBe(QuitChoiceFactory.CreateStayOpen());
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_question_is_asked_twice_then_both_wait_for_the_same_answer()
    {
        var dialog = Create();
        var first = dialog.AskAsync(CancellationToken);
        var second = dialog.AskAsync(CancellationToken);

        dialog.QuitCommand.Execute(null);

        (await first, await second).ShouldBe((QuitChoiceFactory.CreateQuitAnyway(), QuitChoiceFactory.CreateQuitAnyway()));
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_answer_is_stay_open_and_the_dialog_closes()
    {
        var dialog = Create();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        var answer = dialog.AskAsync(source.Token);

        await source.CancelAsync();

        (await answer).ShouldBe(QuitChoiceFactory.CreateStayOpen());
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void when_the_texts_are_read_then_they_come_from_localisation()
    {
        var dialog = Create();

        (dialog.TitleText, dialog.MessageText, dialog.QuitText, dialog.StayText).ShouldBe(("Quit while syncing?", "A sync is running. If you quit now it is cancelled and carries on the next time CloudSync starts.", "Cancel the sync and quit", "Keep syncing"));
    }

    [Fact]
    public void when_the_answer_is_given_with_nothing_asked_then_nothing_happens()
    {
        var dialog = Create();

        dialog.QuitCommand.Execute(null);

        dialog.IsOpen.ShouldBeFalse();
    }
}
