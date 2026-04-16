using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Onboarding;

public sealed class GivenAnAddAccountWizardViewModel
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();

    private AddAccountWizardViewModel CreateSut() => new(_authService, _graphService);

    private async Task SignInAsync(AddAccountWizardViewModel sut)
    {
        _authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResult.Success("fake-token", "account-1", "Test User", "test@example.com"));

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
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
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
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
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
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                stepAtFolderLoad = sut.CurrentStep;
                return Task.FromResult(new List<DriveFolder>());
            });

        await sut.NextCommand.ExecuteAsync(null);

        stepAtFolderLoad.ShouldBe(WizardStep.SelectFolders);
    }

    [Fact]
    public async Task when_next_clicked_on_sign_in_step_then_is_loading_folders_is_true_during_load()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        var wasLoadingDuringFetch = false;
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                wasLoadingDuringFetch = sut.IsLoadingFolders;
                return Task.FromResult(new List<DriveFolder>());
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
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new DriveFolder("id-1", "Documents"), new DriveFolder("id-2", "Pictures")]);

        await sut.NextCommand.ExecuteAsync(null);

        sut.Folders.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_folders_load_then_documents_and_desktop_are_pre_selected()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new DriveFolder("id-1", "Documents"), new DriveFolder("id-2", "Desktop"), new DriveFolder("id-3", "Pictures")]);

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
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<List<DriveFolder>>(_ => throw new InvalidOperationException("network error"));

        await sut.NextCommand.ExecuteAsync(null);

        sut.FolderLoadError.ShouldNotBeNullOrEmpty();
        sut.IsLoadingFolders.ShouldBeFalse();
    }

    [Fact]
    public async Task when_skip_folders_clicked_then_advances_to_confirm_step()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        await sut.NextCommand.ExecuteAsync(null);

        sut.SkipFoldersCommand.Execute(null);

        sut.CurrentStep.ShouldBe(WizardStep.Confirm);
    }

    [Fact]
    public async Task when_skip_folders_clicked_then_confirmed_folder_count_is_zero()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new DriveFolder("id-1", "Documents")]);
        await sut.NextCommand.ExecuteAsync(null);

        sut.SkipFoldersCommand.Execute(null);

        sut.ConfirmedFolderCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_next_clicked_on_select_folders_step_then_confirmed_folder_count_reflects_selection()
    {
        var sut = CreateSut();
        await SignInAsync(sut);
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new DriveFolder("id-1", "Documents"), new DriveFolder("id-2", "Pictures")]);
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
        _graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new DriveFolder("id-1", "Documents"), new DriveFolder("id-2", "Pictures")]);
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
    public void when_on_confirm_step_then_next_label_is_finish()
    {
        var sut = CreateSut();
        sut.CurrentStep = WizardStep.Confirm;

        sut.NextLabel.ShouldBe("Finish");
    }

    [Fact]
    public void when_not_on_confirm_step_then_next_label_is_next()
    {
        var sut = CreateSut();

        sut.NextLabel.ShouldBe("Next");
    }
}
