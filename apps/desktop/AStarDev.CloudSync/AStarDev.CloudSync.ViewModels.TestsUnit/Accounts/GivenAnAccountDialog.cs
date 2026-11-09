using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountDialog
{
    private readonly FakeAccountActions actions = new();
    private readonly AccountDialogViewModel dialog;
    private readonly AccountViewModel account;

    public GivenAnAccountDialog()
    {
        dialog = TestData.CreateAccountDialog(actions);
        account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), dialog);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void when_removal_is_asked_then_the_confirmation_names_the_account_and_says_local_files_are_kept()
    {
        _ = dialog.RemoveAsync(account, CancellationToken);

        (dialog.IsOpen, dialog.IsConfirming, dialog.TitleText, dialog.ConfirmText).ShouldBe((true, true, Text("AccountDialog.Remove.Title"), TestData.CreateLocalization().GetLocal("AccountDialog.Remove.Prompt", "Personal", "personal@example.com")));
        dialog.ConfirmText.ShouldContain("local files are kept");
        actions.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_removal_is_confirmed_then_the_account_is_removed_and_the_dialog_closes()
    {
        var shown = dialog.RemoveAsync(account, CancellationToken);

        await dialog.ConfirmRemoveCommand.ExecuteAsync(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (dialog.IsOpen, actions.Calls.Single()).ShouldBe((false, ("remove", account.Id)));
    }

    [Fact]
    public async Task when_removal_is_cancelled_then_nothing_is_removed_and_the_dialog_closes()
    {
        var shown = dialog.RemoveAsync(account, CancellationToken);

        dialog.CloseCommand.Execute(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (dialog.IsOpen, actions.Calls.Count).ShouldBe((false, 0));
    }

    public static TheoryData<string, string> RemovalFailures => new()
    {
        { "NotFound", "AccountDialog.Remove.Failure.NotFound" },
        { "SyncStillRunning", "AccountDialog.Failure.SyncStillRunning" },
        { "Store", "AccountDialog.Remove.Failure.Store" },
        { "Provider", "AccountDialog.Remove.Failure.Provider" },
    };

    [Theory]
    [MemberData(nameof(RemovalFailures))]
    public async Task when_removal_fails_then_the_localised_failure_shows_until_closed(string failureName, string messageKey)
    {
        actions.Remove = _ => Task.FromResult(Result.Failure<Unit, AccountRemovalFailure>(RemovalFailure(failureName)));
        var shown = dialog.RemoveAsync(account, CancellationToken);

        await dialog.ConfirmRemoveCommand.ExecuteAsync(null);

        (dialog.IsShowingMessage, dialog.MessageText, shown.IsCompleted).ShouldBe((true, Text(messageKey), false));
        dialog.CloseCommand.Execute(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
    }

    [Fact]
    public async Task when_removal_is_running_then_the_dialog_shows_progress_and_cannot_be_closed()
    {
        var removal = new TaskCompletionSource<Result<Unit, AccountRemovalFailure>>(TaskCreationOptions.RunContinuationsAsynchronously);
        actions.Remove = _ => removal.Task;
        var shown = dialog.RemoveAsync(account, CancellationToken);

        var confirming = dialog.ConfirmRemoveCommand.ExecuteAsync(null);
        dialog.CloseCommand.Execute(null);
        var whileWorking = (dialog.IsWorking, dialog.IsOpen, dialog.WorkingText);
        removal.SetResult(Result.Success<Unit, AccountRemovalFailure>(Unit.Instance));
        await confirming;
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        whileWorking.ShouldBe((true, true, TestData.CreateLocalization().GetLocal("AccountDialog.Removing", "Personal")));
    }

    [Fact]
    public async Task when_the_dialog_is_already_open_then_a_second_request_is_ignored()
    {
        var shown = dialog.RemoveAsync(account, CancellationToken);

        await dialog.SignInAsync(account, CancellationToken);

        (dialog.IsConfirming, actions.Calls.Count).ShouldBe((true, 0));
        dialog.CloseCommand.Execute(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("Cancelled")]
    public async Task when_sign_in_succeeds_or_is_cancelled_then_the_dialog_closes_without_a_message(string outcome)
    {
        actions.Reauthenticate = _ => Task.FromResult(outcome == "success" ? Result.Success<Unit, ReauthFailure>(Unit.Instance) : Result.Failure<Unit, ReauthFailure>(ReauthFailureFactory.CreateCancelled()));

        await dialog.SignInAsync(account, CancellationToken);

        (dialog.IsOpen, actions.Calls.Single()).ShouldBe((false, ("sign-in", account.Id)));
    }

    public static TheoryData<string, string> SignInFailures => new()
    {
        { "DifferentAccount", "AccountDialog.SignIn.Failure.DifferentAccount" },
        { "SyncStillRunning", "AccountDialog.Failure.SyncStillRunning" },
        { "Provider", "AccountDialog.SignIn.Failure.Provider" },
    };

    [Theory]
    [MemberData(nameof(SignInFailures))]
    public async Task when_sign_in_fails_then_the_localised_failure_shows(string failureName, string messageKey)
    {
        actions.Reauthenticate = _ => Task.FromResult(Result.Failure<Unit, ReauthFailure>(SignInFailure(failureName)));

        await dialog.SignInAsync(account, CancellationToken);

        (dialog.IsShowingMessage, dialog.TitleText, dialog.MessageText).ShouldBe((true, Text("AccountDialog.SignIn.Title"), TestData.CreateLocalization().GetLocal(messageKey, "personal@example.com")));
    }

    [Fact]
    public async Task when_removal_throws_then_a_failure_shows_and_the_dialog_can_be_closed()
    {
        actions.Remove = _ => throw new InvalidOperationException($"Broken for {account.Email}");
        var shown = dialog.RemoveAsync(account, CancellationToken);

        await dialog.ConfirmRemoveCommand.ExecuteAsync(null);

        (dialog.IsShowingMessage, dialog.MessageText).ShouldBe((true, Text("AccountDialog.Remove.Failure.Provider")));
        dialog.CloseCommand.Execute(null);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
    }

    [Fact]
    public async Task when_sign_in_throws_then_a_failure_shows_instead_of_staying_busy()
    {
        actions.Reauthenticate = _ => throw new InvalidOperationException("MSAL broke");

        await dialog.SignInAsync(account, CancellationToken);

        (dialog.IsWorking, dialog.IsShowingMessage, dialog.MessageText).ShouldBe((false, true, TestData.CreateLocalization().GetLocal("AccountDialog.SignIn.Failure.Provider", "personal@example.com")));
    }

    [Fact]
    public void when_a_dialog_step_is_printed_then_its_text_is_redacted()
    {
        var steps = new[] { AccountDialogStepFactory.CreateWorking("Removing personal@example.com"), AccountDialogStepFactory.CreateMessage("Sign in as personal@example.com"), AccountDialogStepFactory.CreateWorking(null) };

        steps.ShouldAllBe(step => !step.ToString().Contains("personal@example.com"));
        steps[2].ShouldBeOfType<AccountDialogStep.Working>().Text.ShouldBeEmpty();
    }

    private static string Text(string key) => TestData.CreateLocalization().GetLocal(key);

    private static AccountRemovalFailure RemovalFailure(string name) => name switch
    {
        "NotFound" => AccountRemovalFailureFactory.CreateNotFound(),
        "SyncStillRunning" => AccountRemovalFailureFactory.CreateSyncStillRunning(),
        "Store" => AccountRemovalFailureFactory.CreateStore(new ErrorResponse("locked")),
        _ => AccountRemovalFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()),
    };

    private static ReauthFailure SignInFailure(string name) => name switch
    {
        "DifferentAccount" => ReauthFailureFactory.CreateDifferentAccount(),
        "SyncStillRunning" => ReauthFailureFactory.CreateSyncStillRunning(),
        _ => ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()),
    };
}
