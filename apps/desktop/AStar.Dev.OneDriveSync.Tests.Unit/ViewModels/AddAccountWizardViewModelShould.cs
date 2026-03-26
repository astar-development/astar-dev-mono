using AStar.Dev.OneDriveSync.Services;
using AStar.Dev.OneDriveSync.ViewModels;

namespace AStar.Dev.OneDriveSync.Tests.Unit.ViewModels;

[TestSubject(typeof(AddAccountWizardViewModel))]
public class AddAccountWizardViewModelShould
{
    private readonly IMsalAuthService _authService = Substitute.For<IMsalAuthService>();
    private readonly IOneDriveFolderService _folderService = Substitute.For<IOneDriveFolderService>();
    private bool _cancelCalled;
    private MsalAuthResult? _finishedAuth;
    private readonly AddAccountWizardViewModel _sut;

    public AddAccountWizardViewModelShould()
    {
        _sut = new AddAccountWizardViewModel(_authService, _folderService, () => _cancelCalled = true, (auth, folders, path) => { _finishedAuth = auth; });
    }

    [Fact]
    public void StartOnSignInStep()
    {
        _sut.IsSignInStep.ShouldBeTrue();
        _sut.IsSelectFoldersStep.ShouldBeFalse();
        _sut.IsConfirmStep.ShouldBeFalse();
    }

    [Fact]
    public void NotAllowNextBeforeSignIn()
        => _sut.CanGoNext.ShouldBeFalse();

    [Fact]
    public void InvokeCancelCallback()
    {
        _sut.CancelCommand.Execute(null);
        _cancelCalled.ShouldBeTrue();
    }

    [Fact]
    public void NotAllowGoBackOnFirstStep()
        => _sut.CanGoBack.ShouldBeFalse();

    [Fact]
    public void ShowNextLabelAsNext_OnStep1()
        => _sut.NextLabel.ShouldBe("Next");
}
