using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.FolderSelection;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.TestsUnit.Overlays;

public class GivenAnOverlayDialogAtAStep
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [AvaloniaFact]
    public async Task when_escape_is_pressed_while_signing_in_then_the_sign_in_is_cancelled_and_the_dialog_closes()
    {
        var adder = new FakeAccountAdder { Add = WaitUntilCancelled };
        var dialog = TestData.CreateAddAccount(adder);
        var window = Show(new AddAccountView(), dialog);
        var shown = dialog.ShowAsync(CancellationToken);
        Dispatcher.UIThread.RunJobs();
        var signingIn = dialog.IsSigningIn;

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (signingIn, dialog.IsOpen, adder.ReceivedTokens.Single().IsCancellationRequested).ShouldBe((true, false, true));
    }

    [AvaloniaFact]
    public async Task when_escape_is_pressed_while_the_plaintext_token_question_shows_then_the_attempt_is_cancelled_and_the_dialog_closes()
    {
        var consent = new TokenCacheConsentViewModel(new ImmediateUiDispatcher(), TestData.CreateLocalization());
        var adder = new FakeAccountAdder { Add = token => AskThenCancelAsync(consent, token) };
        var dialog = new AddAccountViewModel(new SingleProviderCapability<IAccountAdder>(adder), consent, new RecordingFolderSelectionDialog(), TestData.CreateLocalization(), NullLogger<AddAccountViewModel>.Instance);
        var window = Show(new AddAccountView(), dialog);
        var shown = dialog.ShowAsync(CancellationToken);
        Dispatcher.UIThread.RunJobs();
        var asking = dialog.IsAskingConsent;

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        await shown.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        (asking, dialog.IsOpen).ShouldBe((true, false));
    }

    [AvaloniaFact]
    public async Task when_escape_is_pressed_on_the_untick_confirmation_then_it_goes_back_to_the_tree_without_saving()
    {
        var tree = new FakeFolderTreeSource { Levels = _ => Result.Success<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>([DocsNode()]) };
        var rules = new FakeSelectionRuleReader { Rules = Result.Success<IReadOnlyList<SelectionRule>, ErrorResponse>([SelectionRuleFactory.Create(Account, RelativePaths.Of("Docs"), Option.None<RemoteItemId>(), SelectionRuleKindFactory.CreateInclude(SyncDirectionFactory.CreateTwoWay()))]) };
        var writer = new FakeSelectionRuleWriter();
        var pair = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), Account, "Docs", RelativePaths.Of("Docs"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncPairStateFactory.Create(SyncStatusFactory.CreateUpToDate(), 0, Option.Some(TestData.Now)));
        var pairs = new FakeSyncPairReader { Pairs = _ => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>([pair]) };
        var dialog = TestData.CreateFolderSelection(tree, rules, writer, pairs, new InMemoryAccountStore());
        var window = Show(new FolderSelectionView(), dialog);
        _ = dialog.ShowAsync(Option.Some(Account), CancellationToken);
        dialog.Roots.Single().ToggleCommand.Execute(null);
        await dialog.ApplyCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        var confirming = dialog.IsConfirming;

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        (confirming, dialog.IsConfirming, dialog.IsOpen, dialog.IsShowingTree, writer.Applied.Count).ShouldBe((true, false, true, true, 0));
    }

    [AvaloniaFact]
    public async Task when_escape_is_pressed_on_the_sync_folder_confirmation_then_the_change_is_cancelled_and_the_dialog_stays_open()
    {
        var actions = new FakeAccountActions();
        var syncRoots = new FakeSyncRootReader();
        var picker = new FakeFolderPicker();
        var dialog = TestData.CreateAccountDialog(actions, syncRoots, new InMemoryAccountStore(), picker);
        var account = TestData.CreateAccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.Some(TestData.Now)), dialog);
        var files = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var current = SyncRootFactory.Create(files.DirectoryInfo.New("/home/test/CloudSync/Personal")).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid."));
        syncRoots.Roots = [AccountSyncRootFactory.Create(account.Id, current)];
        picker.Choice = Option.Some(files.DirectoryInfo.New("/data/sync"));
        var window = Show(new AccountDialogView(), dialog);
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);
        await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        var confirming = dialog.IsConfirmingSyncFolderChange;

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        (confirming, dialog.IsConfirmingSyncFolderChange, dialog.IsOpen, actions.Calls.Count).ShouldBe((true, false, true, 0));
    }

    private static async Task<AccountAddOutcome> AskThenCancelAsync(TokenCacheConsentViewModel consent, CancellationToken token)
    {
        var asked = consent.AskAsync(token);
        await ((Task)asked).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return AccountAddOutcomeFactory.CreateCancelled();
    }

    private static Task<AccountAddOutcome> WaitUntilCancelled(CancellationToken token)
    {
        var done = new TaskCompletionSource<AccountAddOutcome>();
        _ = token.Register(() => done.TrySetResult(AccountAddOutcomeFactory.CreateCancelled()));

        return done.Task;
    }

    private static FolderTreeNode DocsNode() =>
        FolderTreeNodeFactory.Create("Docs", RelativePaths.Of("Docs"), Option.None<RemoteItemId>(), FolderOriginFactory.CreateBoth(), FolderChildrenFactory.CreateNoChildFolders(), SelectionStateFactory.CreateUncovered(), FolderNameClashFactory.CreateUnique());

    private static Window Show(Control view, object dataContext)
    {
        view.DataContext = dataContext;
        var window = new Window { Width = 1000, Height = 800, Content = new StackPanel { Children = { new Button { Content = "Opener" }, view } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
