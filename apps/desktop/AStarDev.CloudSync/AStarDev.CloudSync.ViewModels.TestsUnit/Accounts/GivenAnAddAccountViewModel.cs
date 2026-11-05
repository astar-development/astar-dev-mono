using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Accounts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAddAccountViewModel
{
    private static readonly AccountId AddedId = AccountIdFactory.Create(Guid.Parse("5e6f7a8b-9c0d-4e1f-a2b3-c4d5e6f7a8b9"));

    [Fact]
    public void when_created_then_the_dialog_is_closed_and_its_texts_are_localised()
    {
        var dialog = TestData.CreateAddAccount(new FakeAccountAdder());

        dialog.IsOpen.ShouldBeFalse();
        dialog.Step.ShouldBeOfType<AddAccountStep.Closed>();
        string[] texts = [dialog.TitleText, dialog.SigningInText, dialog.CancelText, dialog.CloseText, dialog.TryAgainText];
        texts.ShouldAllBe(text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("AddAccount."));
    }

    [Fact]
    public async Task when_the_dialog_opens_then_it_shows_the_signing_in_panel_while_the_user_signs_in()
    {
        var signIn = new TaskCompletionSource<AccountAddOutcome>();
        var dialog = TestData.CreateAddAccount(new FakeAccountAdder { Add = _ => signIn.Task });

        var shown = dialog.ShowAsync(TestContext.Current.CancellationToken);

        (dialog.IsOpen, dialog.IsSigningIn, dialog.IsAskingConsent, dialog.IsShowingMessage).ShouldBe((true, true, false, false));
        signIn.SetResult(AccountAddOutcomeFactory.CreateAdded(AddedId));
        await shown;
    }

    [Fact]
    public async Task when_a_new_account_is_added_then_the_dialog_closes_by_itself()
    {
        var dialog = TestData.CreateAddAccount(new FakeAccountAdder { Add = _ => Task.FromResult(AccountAddOutcomeFactory.CreateAdded(AddedId)) });

        await dialog.ShowAsync(TestContext.Current.CancellationToken);

        (dialog.IsOpen, dialog.Step).ShouldBe((false, AddAccountStepFactory.CreateClosed()));
    }

    [Fact]
    public async Task when_the_attempt_is_cancelled_then_the_dialog_closes_by_itself()
    {
        var dialog = TestData.CreateAddAccount(new FakeAccountAdder { Add = _ => Task.FromResult(AccountAddOutcomeFactory.CreateCancelled()) });

        await dialog.ShowAsync(TestContext.Current.CancellationToken);

        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_account_was_already_added_then_the_notice_shows_until_the_user_closes_the_dialog()
    {
        var dialog = TestData.CreateAddAccount(new FakeAccountAdder { Add = _ => Task.FromResult(AccountAddOutcomeFactory.CreateReAuthenticated(AddedId)) });

        var shown = dialog.ShowAsync(TestContext.Current.CancellationToken);

        (dialog.Step, dialog.IsShowingMessage, dialog.CanTryAgain, shown.IsCompleted).ShouldBe((AddAccountStepFactory.CreateAlreadyAdded(), true, false, false));
        dialog.MessageText.ShouldBe(TestData.CreateLocalization().GetLocal("AddAccount.AlreadyAdded"));
        dialog.CloseCommand.Execute(null);
        await shown;
        dialog.IsOpen.ShouldBeFalse();
    }

    public static TheoryData<AccountAddFailure, string> FailuresAndMessageKeys() => new()
    {
        { AccountAddFailureFactory.CreateBrowserUnavailable(), "AddAccount.Failure.BrowserUnavailable" },
        { AccountAddFailureFactory.CreateAuthRejected(), "AddAccount.Failure.AuthRejected" },
        { AccountAddFailureFactory.CreateNetwork(), "AddAccount.Failure.Network" },
        { AccountAddFailureFactory.CreateTokenCacheUnavailable(), "AddAccount.Failure.TokenCacheUnavailable" },
        { AccountAddFailureFactory.CreateDriveUnavailable(), "AddAccount.Failure.DriveUnavailable" },
        { AccountAddFailureFactory.CreatePersistFailed(), "AddAccount.Failure.PersistFailed" },
    };

    [Theory]
    [MemberData(nameof(FailuresAndMessageKeys))]
    public async Task when_the_attempt_fails_then_the_localised_failure_message_shows_with_try_again(AccountAddFailure failure, string messageKey)
    {
        var localization = TestData.CreateLocalization();
        var dialog = TestData.CreateAddAccount(new FakeAccountAdder { Add = _ => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(failure)) });

        var shown = dialog.ShowAsync(TestContext.Current.CancellationToken);

        (dialog.Step, dialog.IsShowingMessage, dialog.CanTryAgain, dialog.IsSigningIn).ShouldBe((AddAccountStepFactory.CreateFailed(failure), true, true, false));
        dialog.MessageText.ShouldBe(localization.GetLocal(messageKey));
        dialog.MessageText.ShouldNotBe(messageKey);
        dialog.CloseCommand.Execute(null);
        await shown;
    }

    [Fact]
    public async Task when_the_user_presses_cancel_while_signing_in_then_the_sign_in_token_is_cancelled_and_the_dialog_closes()
    {
        var adder = new FakeAccountAdder { Add = async token => { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing); return AccountAddOutcomeFactory.CreateCancelled(); } };
        var dialog = TestData.CreateAddAccount(adder);

        var shown = dialog.ShowAsync(TestContext.Current.CancellationToken);
        dialog.CancelCommand.Execute(null);
        await shown;

        (adder.ReceivedTokens.ShouldHaveSingleItem().IsCancellationRequested, dialog.IsOpen).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_caller_cancels_then_the_sign_in_token_is_cancelled_too()
    {
        using var cancellation = new CancellationTokenSource();
        var adder = new FakeAccountAdder { Add = async token => { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing); return AccountAddOutcomeFactory.CreateCancelled(); } };
        var dialog = TestData.CreateAddAccount(adder);

        var shown = dialog.ShowAsync(cancellation.Token);
        await cancellation.CancelAsync();
        await shown;

        adder.ReceivedTokens.ShouldHaveSingleItem().IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_user_tries_again_after_a_failure_then_a_new_attempt_runs()
    {
        var outcomes = new Queue<AccountAddOutcome>([AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()), AccountAddOutcomeFactory.CreateAdded(AddedId)]);
        var adder = new FakeAccountAdder { Add = _ => Task.FromResult(outcomes.Dequeue()) };
        var dialog = TestData.CreateAddAccount(adder);

        var shown = dialog.ShowAsync(TestContext.Current.CancellationToken);
        await dialog.TryAgainCommand.ExecuteAsync(null);
        await shown;

        (adder.ReceivedTokens.Count, dialog.IsOpen).ShouldBe((2, false));
    }

    [Fact]
    public async Task when_the_dialog_is_shown_again_while_open_then_no_second_attempt_starts()
    {
        var signIn = new TaskCompletionSource<AccountAddOutcome>();
        var adder = new FakeAccountAdder { Add = _ => signIn.Task };
        var dialog = TestData.CreateAddAccount(adder);

        var first = dialog.ShowAsync(TestContext.Current.CancellationToken);
        var second = dialog.ShowAsync(TestContext.Current.CancellationToken);
        signIn.SetResult(AccountAddOutcomeFactory.CreateAdded(AddedId));
        await Task.WhenAll(first, second);

        adder.ReceivedTokens.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_plaintext_token_question_shows_then_the_consent_panel_replaces_the_signing_in_panel()
    {
        var consent = new TokenCacheConsentViewModel(new ImmediateUiDispatcher(), TestData.CreateLocalization());
        var adder = new FakeAccountAdder { Add = async token => { _ = await consent.AskAsync(token); return AccountAddOutcomeFactory.CreateCancelled(); } };
        var dialog = new AddAccountViewModel(adder, consent, TestData.CreateLocalization());

        var shown = dialog.ShowAsync(TestContext.Current.CancellationToken);

        (dialog.IsAskingConsent, dialog.IsSigningIn).ShouldBe((true, false));
        consent.DeclineCommand.Execute(null);
        await shown;
    }

    [Fact]
    public async Task when_the_step_changes_then_the_dependent_properties_are_raised()
    {
        var dialog = TestData.CreateAddAccount(new FakeAccountAdder { Add = _ => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork())) });
        var changed = new HashSet<string?>();
        dialog.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        var shown = dialog.ShowAsync(TestContext.Current.CancellationToken);
        dialog.CloseCommand.Execute(null);
        await shown;

        string[] expected = [nameof(AddAccountViewModel.Step), nameof(AddAccountViewModel.IsOpen), nameof(AddAccountViewModel.IsSigningIn), nameof(AddAccountViewModel.IsShowingMessage), nameof(AddAccountViewModel.CanTryAgain), nameof(AddAccountViewModel.MessageText)];
        expected.ShouldAllBe(name => changed.Contains(name));
    }
}
