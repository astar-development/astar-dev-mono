using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenATokenCacheConsentViewModel
{
    private readonly ImmediateUiDispatcher dispatcher = new();

    [Fact]
    public void when_created_then_no_question_is_showing_and_the_texts_are_localised()
    {
        var consent = CreateConsent();

        consent.IsAsking.ShouldBeFalse();
        string[] texts = [consent.TitleText, consent.MessageText, consent.AgreeText, consent.DeclineText];
        texts.ShouldAllBe(text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("TokenCacheConsent."));
    }

    [Fact]
    public async Task when_the_question_is_asked_then_it_shows_through_the_ui_dispatcher_until_answered()
    {
        var consent = CreateConsent();

        var answer = consent.AskAsync(TestContext.Current.CancellationToken);

        (consent.IsAsking, answer.IsCompleted, dispatcher.Invocations > 0).ShouldBe((true, false, true));
        consent.AgreeCommand.Execute(null);
        _ = await answer;
    }

    [Fact]
    public async Task when_the_user_agrees_then_the_answer_is_granted_and_the_question_closes()
    {
        var consent = CreateConsent();

        var answer = consent.AskAsync(TestContext.Current.CancellationToken);
        consent.AgreeCommand.Execute(null);

        (await answer).ShouldBeOfType<PlaintextCacheConsent.Granted>();
        consent.IsAsking.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_user_declines_then_the_answer_is_declined_and_the_question_closes()
    {
        var consent = CreateConsent();

        var answer = consent.AskAsync(TestContext.Current.CancellationToken);
        consent.DeclineCommand.Execute(null);

        (await answer).ShouldBeOfType<PlaintextCacheConsent.Declined>();
        consent.IsAsking.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_question_is_cancelled_then_it_closes_and_the_cancellation_is_not_swallowed()
    {
        using var cancellation = new CancellationTokenSource();
        var consent = CreateConsent();

        var answer = consent.AskAsync(cancellation.Token);
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => answer);
        consent.IsAsking.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_question_shows_or_closes_then_is_asking_change_is_raised()
    {
        var consent = CreateConsent();
        var changed = new List<string?>();
        consent.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        var answer = consent.AskAsync(TestContext.Current.CancellationToken);
        consent.DeclineCommand.Execute(null);
        _ = await answer;

        changed.Count(name => name == nameof(TokenCacheConsentViewModel.IsAsking)).ShouldBe(2);
    }

    [Fact]
    public void when_nobody_is_asking_then_the_answer_buttons_do_nothing()
    {
        var consent = CreateConsent();

        consent.AgreeCommand.Execute(null);
        consent.DeclineCommand.Execute(null);

        consent.IsAsking.ShouldBeFalse();
    }

    private TokenCacheConsentViewModel CreateConsent() => new(dispatcher, TestData.CreateLocalization());
}
