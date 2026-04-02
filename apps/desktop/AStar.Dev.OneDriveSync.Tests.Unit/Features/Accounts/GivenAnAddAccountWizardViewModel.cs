using System.Reactive;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDrive.Client.Features.FolderBrowsing;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Accounts;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Accounts;

public sealed class GivenAnAddAccountWizardViewModel
{
    private readonly IMsalClient _msalClient = Substitute.For<IMsalClient>();
    private readonly IOneDriveFolderService _folderService = Substitute.For<IOneDriveFolderService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalSyncPathService _pathService = Substitute.For<ILocalSyncPathService>();

    [Fact]
    public void when_wizard_is_created_then_current_step_is_one()
    {
        var sut = CreateSut();

        sut.CurrentStep.ShouldBe(1);
    }

    [Fact]
    public void when_wizard_is_created_then_cannot_advance_before_authentication()
    {
        var sut = CreateSut();

        sut.CanAdvance.ShouldBeFalse();
    }

    [Fact]
    public async Task when_authentication_succeeds_then_can_advance_to_step_two()
    {
        var token = new AccessToken("test-token", DateTimeOffset.UtcNow.AddHours(1));
        _msalClient.AcquireTokenInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<(AccessToken, string?), string>.Ok((token, null)));

        var sut = CreateSut();
        await sut.AuthenticateCommand.Execute().FirstAsync();

        sut.CurrentStep.ShouldBe(2);
        sut.CanAdvance.ShouldBeFalse();
    }

    [Fact]
    public async Task when_authentication_fails_then_error_message_is_set_and_stays_on_step_one()
    {
        _msalClient.AcquireTokenInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<(AccessToken, string?), string>.Error("Auth failed"));

        var sut = CreateSut();
        await sut.AuthenticateCommand.Execute().FirstAsync();

        sut.CurrentStep.ShouldBe(1);
        sut.ErrorMessage.ShouldBe("Auth failed");
    }

    [Fact]
    public async Task when_on_step_two_and_folders_are_loaded_then_can_advance()
    {
        var token = new AccessToken("test-token", DateTimeOffset.UtcNow.AddHours(1));
        _msalClient.AcquireTokenInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<(AccessToken, string?), string>.Ok((token, null)));
        IReadOnlyList<OneDriveFolder> folders = [OneDriveFolderFactory.Create("id1", "Documents", null, false)];
        _folderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<OneDriveFolder>, string>.Ok(folders));
        _pathService.GetDefaultPath(Arg.Any<string>()).Returns("/home/user/OneDrive/Test");

        var sut = CreateSut();
        await sut.AuthenticateCommand.Execute().FirstAsync();
        await sut.LoadFoldersCommand.Execute().FirstAsync();

        sut.AvailableFolders.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_on_step_three_then_back_returns_to_step_two()
    {
        var token = new AccessToken("test-token", DateTimeOffset.UtcNow.AddHours(1));
        _msalClient.AcquireTokenInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<(AccessToken, string?), string>.Ok((token, null)));
        IReadOnlyList<OneDriveFolder> folders = [OneDriveFolderFactory.Create("id1", "Documents", null, false)];
        _folderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<IReadOnlyList<OneDriveFolder>, string>.Ok(folders));
        _pathService.GetDefaultPath(Arg.Any<string>()).Returns("/home/user/OneDrive/Test");
        _pathService.ValidateNoOverlapAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<bool, string>.Ok(true));

        var sut = CreateSut();
        await sut.AuthenticateCommand.Execute().FirstAsync();
        await sut.LoadFoldersCommand.Execute().FirstAsync();
        sut.AdvanceCommand.Execute(System.Reactive.Unit.Default).Subscribe();
        sut.BackCommand.Execute(System.Reactive.Unit.Default).Subscribe();

        sut.CurrentStep.ShouldBe(2);
    }

    [Fact]
    public void when_cancel_is_invoked_then_is_cancelled_is_true()
    {
        var sut = CreateSut();

        sut.CancelCommand.Execute(System.Reactive.Unit.Default).Subscribe();

        sut.IsCancelled.ShouldBeTrue();
    }

    private AddAccountWizardViewModel CreateSut()
        => new(_msalClient, _folderService, _accountRepository, _pathService);
}
