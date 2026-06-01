using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Onboarding;

public sealed class GivenAnAddAccountWizardViewModel
{
    private sealed record UnknownAuthError : AuthError;

    private readonly IAuthService         _authService         = Substitute.For<IAuthService>();
    private readonly IGraphService        _graphService        = Substitute.For<IGraphService>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private AddAccountWizardViewModel CreateSut() => new(_authService, _graphService, _localizationService);

    private async Task SignInAsync(AddAccountWizardViewModel sut)
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("fake-token", "account-1", AccountProfileFactory.Create("Test User", "test@example.com")));

        await sut.OpenBrowserCommand.ExecuteAsync(null);
    }

    [Fact]
    public void when_created_then_starts_on_sign_in_step()
    {
        var sut = CreateSut();

        sut.CurrentStep.ShouldBe(WizardStep.SignIn);
    }

    [Fact]
    public void when_created_then_cannot_go_back()
    {
        var sut = CreateSut();

        sut.CanGoBack.ShouldBeFalse();
    }

    [Fact]
    public void when_created_then_cannot_go_next_until_signed_in()
    {
        var sut = CreateSut();

        sut.CanGoNext.ShouldBeFalse();
    }

    [Fact]
    public async Task when_cancel_clicked_on_sign_in_step_then_cancelled_event_fires()
    {
        var sut = CreateSut();
        EventArgs? firedArgs = null;
        sut.Cancelled += (_, args) => firedArgs = args;

        await sut.CancelCommand.ExecuteAsync(null);

        firedArgs.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_cancel_clicked_on_select_folders_step_then_cancelled_event_fires()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<List<DriveFolder>, string>.Ok([]));
        await sut.NextCommand.ExecuteAsync(null);
        EventArgs? firedArgs = null;
        sut.Cancelled += (_, args) => firedArgs = args;

        await sut.CancelCommand.ExecuteAsync(null);

        firedArgs.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_cancel_clicked_on_confirm_step_then_cancelled_event_fires()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<List<DriveFolder>, string>.Ok([]));
        await sut.NextCommand.ExecuteAsync(null);
        await sut.NextCommand.ExecuteAsync(null);
        EventArgs? firedArgs = null;
        sut.Cancelled += (_, args) => firedArgs = args;

        await sut.CancelCommand.ExecuteAsync(null);

        firedArgs.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_next_clicked_on_sign_in_step_then_step_changes_before_folders_load()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        var stepAtFolderLoad = WizardStep.SignIn;
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                stepAtFolderLoad = sut.CurrentStep;
                return Task.FromResult<Result<List<DriveFolder>, string>>(new Result<List<DriveFolder>, string>.Ok([]));
            });

        await sut.NextCommand.ExecuteAsync(null);

        stepAtFolderLoad.ShouldBe(WizardStep.SelectFolders);
    }

    [Fact]
    public async Task when_next_clicked_on_sign_in_step_then_is_loading_folders_is_true_during_load()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        bool wasLoadingDuringFetch = false;
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                wasLoadingDuringFetch = sut.IsLoadingFolders;
                return Task.FromResult<Result<List<DriveFolder>, string>>(new Result<List<DriveFolder>, string>.Ok([]));
            });

        await sut.NextCommand.ExecuteAsync(null);

        wasLoadingDuringFetch.ShouldBeTrue();
        sut.IsLoadingFolders.ShouldBeFalse();
    }

    [Fact]
    public async Task when_folders_load_then_folders_are_populated()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder("id-1", "Documents", Option.None<string>()), new DriveFolder("id-2", "Pictures", Option.None<string>())]));

        await sut.NextCommand.ExecuteAsync(null);

        sut.Folders.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_folders_load_then_documents_and_desktop_are_pre_selected()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder("id-1", "Documents", Option.None<string>()), new DriveFolder("id-2", "Desktop", Option.None<string>()), new DriveFolder("id-3", "Pictures", Option.None<string>())]));

        await sut.NextCommand.ExecuteAsync(null);

        sut.Folders.Single(f => f.Name == "Documents").IsSelected.ShouldBeTrue();
        sut.Folders.Single(f => f.Name == "Desktop").IsSelected.ShouldBeTrue();
        sut.Folders.Single(f => f.Name == "Pictures").IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task when_graph_service_throws_then_folder_load_error_is_set()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Error("network error"));

        await sut.NextCommand.ExecuteAsync(null);

        sut.FolderLoadError.ShouldNotBeNullOrEmpty();
        sut.IsLoadingFolders.ShouldBeFalse();
    }

    [Fact]
    public async Task when_skip_folders_clicked_then_advances_to_confirm_step()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<List<DriveFolder>, string>.Ok([]));
        await sut.NextCommand.ExecuteAsync(null);

        sut.SkipFoldersCommand.Execute(null);

        sut.CurrentStep.ShouldBe(WizardStep.Confirm);
    }

    [Fact]
    public async Task when_skip_folders_clicked_then_confirmed_folder_count_is_zero()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder("id-1", "Documents", Option.None<string>())]));
        await sut.NextCommand.ExecuteAsync(null);

        sut.SkipFoldersCommand.Execute(null);

        sut.ConfirmedFolderCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_next_clicked_on_select_folders_step_then_confirmed_folder_count_reflects_selection()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder("id-1", "Documents", Option.None<string>()), new DriveFolder("id-2", "Pictures", Option.None<string>())]));
        await sut.NextCommand.ExecuteAsync(null);
        sut.Folders[1].IsSelected = false;

        await sut.NextCommand.ExecuteAsync(null);

        sut.ConfirmedFolderCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_finish_clicked_then_completed_event_fires_with_selected_folders()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder("id-1", "Documents", Option.None<string>()), new DriveFolder("id-2", "Pictures", Option.None<string>())]));
        await sut.NextCommand.ExecuteAsync(null);
        sut.Folders[1].IsSelected = false;
        await sut.NextCommand.ExecuteAsync(null);
        OneDriveAccount? completedAccount = null;
        sut.Completed += (_, account) => completedAccount = account;

        await sut.NextCommand.ExecuteAsync(null);

        completedAccount.ShouldNotBeNull();
        completedAccount.SelectedFolderIds.Count.ShouldBe(1);
    }

    [Fact]
    public void when_back_clicked_on_select_folders_then_returns_to_sign_in()
    {
        var sut = CreateSut();
        sut.CurrentStep = WizardStep.SelectFolders;

        sut.BackCommand.Execute(null);

        sut.CurrentStep.ShouldBe(WizardStep.SignIn);
    }

    [Fact]
    public void when_back_clicked_on_confirm_then_returns_to_select_folders()
    {
        var sut = CreateSut();
        sut.CurrentStep = WizardStep.Confirm;

        sut.BackCommand.Execute(null);

        sut.CurrentStep.ShouldBe(WizardStep.SelectFolders);
    }

    [Fact]
    public void when_current_step_is_confirm_then_next_label_returns_finish_key_value()
    {
        _localizationService.GetLocal("Wizard.AddAccount.Finish").Returns("test-finish");
        var sut = CreateSut();
        sut.CurrentStep = WizardStep.Confirm;

        sut.NextLabel.ShouldBe("test-finish");
    }

    [Fact]
    public void when_current_step_is_not_confirm_then_next_label_returns_next_key_value()
    {
        _localizationService.GetLocal("Wizard.AddAccount.Next").Returns("test-next");
        var sut = CreateSut();

        sut.NextLabel.ShouldBe("test-next");
    }

    [Fact]
    public async Task when_sign_in_succeeds_then_is_signed_in_is_true()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "acc-1", AccountProfileFactory.Create("Test User", "test@example.com")));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.IsSignedIn.ShouldBeTrue();
    }

    [Fact]
    public async Task when_sign_in_succeeds_then_sign_in_status_text_contains_email()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "acc-1", AccountProfileFactory.Create("Test User", "test@example.com")));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInStatusText.ShouldContain("test@example.com");
    }

    [Fact]
    public async Task when_sign_in_succeeds_then_sign_in_has_error_is_false()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "acc-1", AccountProfileFactory.Create("Test User", "test@example.com")));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInHasError.ShouldBeFalse();
    }

    [Fact]
    public async Task when_sign_in_succeeds_then_confirmed_display_name_is_set()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "acc-1", AccountProfileFactory.Create("Test User", "test@example.com")));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.ConfirmedDisplayName.ShouldBe("Test User");
    }

    [Fact]
    public async Task when_sign_in_succeeds_then_confirmed_email_is_set()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success("token", "acc-1", AccountProfileFactory.Create("Test User", "test@example.com")));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.ConfirmedEmail.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task when_sign_in_is_cancelled_then_is_signed_in_remains_false()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.IsSignedIn.ShouldBeFalse();
    }

    [Fact]
    public async Task when_sign_in_is_cancelled_then_sign_in_status_text_is_sign_in_cancelled()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInStatusText.ShouldBe("Sign-in cancelled.");
    }

    [Fact]
    public async Task when_sign_in_is_cancelled_then_sign_in_has_error_is_false()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Cancelled());
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInHasError.ShouldBeFalse();
    }

    [Fact]
    public async Task when_sign_in_fails_then_sign_in_status_text_is_the_failure_message()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("MSAL token request failed"));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInStatusText.ShouldBe("MSAL token request failed");
    }

    [Fact]
    public async Task when_sign_in_fails_then_sign_in_has_error_is_true()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("MSAL token request failed"));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInHasError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_sign_in_fails_then_is_signed_in_remains_false()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Failure("MSAL token request failed"));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.IsSignedIn.ShouldBeFalse();
    }

    [Fact]
    public async Task when_sign_in_is_in_progress_and_open_browser_is_called_again_then_second_call_is_ignored()
    {
        var tcs = new TaskCompletionSource<AStar.Dev.Functional.Extensions.Result<AuthResult, AuthError>>();
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(_ => tcs.Task);
        var sut = CreateSut();

        var firstCall = sut.OpenBrowserCommand.ExecuteAsync(null);
        await sut.OpenBrowserCommand.ExecuteAsync(null);
        tcs.SetResult(AuthResultFactory.Success("token", "acc-1", AccountProfileFactory.Create("User", "user@example.com")));
        await firstCall;

        await _authService.Received(1).SignInInteractiveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sign_in_returns_unknown_error_type_then_sign_in_has_error_is_true()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AuthResult, AuthError>.Error(new UnknownAuthError()));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInHasError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_sign_in_returns_unknown_error_type_then_sign_in_status_text_is_set()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AuthResult, AuthError>.Error(new UnknownAuthError()));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.SignInStatusText.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task when_sign_in_returns_unknown_error_type_then_is_signed_in_remains_false()
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AuthResult, AuthError>.Error(new UnknownAuthError()));
        var sut = CreateSut();

        await sut.OpenBrowserCommand.ExecuteAsync(null);

        sut.IsSignedIn.ShouldBeFalse();
    }
}
