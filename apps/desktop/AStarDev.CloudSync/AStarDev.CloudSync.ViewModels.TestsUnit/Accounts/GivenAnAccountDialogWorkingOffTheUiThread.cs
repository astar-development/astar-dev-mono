using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public sealed class GivenAnAccountDialogWorkingOffTheUiThread
{
    private readonly FakeAccountActions actions = new();
    private readonly FakeFolderPicker folderPicker = new();
    private readonly AccountDialogViewModel dialog;
    private readonly AccountViewModel account;

    public GivenAnAccountDialogWorkingOffTheUiThread()
    {
        dialog = TestData.CreateAccountDialog(actions, new FakeSyncRootReader(), new InMemoryAccountStore(), folderPicker);
        account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), dialog);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_sign_in_finishes_on_a_pool_thread_then_the_dialog_changes_on_the_ui_thread()
    {
        actions.Reauthenticate = async _ => await Later(Result.Success<Unit, ReauthFailure>(Unit.Instance));
        using var ui = new SingleThreadContext();
        var threads = Watch();

        await ui.RunAsync(() => dialog.SignInAsync(account, CancellationToken)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
    }

    [Fact]
    public async Task when_sign_in_throws_on_a_pool_thread_then_the_failure_shows_on_the_ui_thread()
    {
        actions.Reauthenticate = async _ => await Later<Result<Unit, ReauthFailure>>(new InvalidOperationException("broke"));
        using var ui = new SingleThreadContext();
        var threads = Watch();

        await ui.RunAsync(() => dialog.SignInAsync(account, CancellationToken)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
        dialog.IsShowingMessage.ShouldBeTrue();
    }

    [Fact]
    public async Task when_removal_finishes_on_a_pool_thread_then_the_dialog_changes_on_the_ui_thread()
    {
        actions.Remove = async _ => await Later(Result.Success<Unit, AccountRemovalFailure>(Unit.Instance));
        using var ui = new SingleThreadContext();
        var threads = Watch();

        await ui.RunAsync(async () =>
        {
            var shown = dialog.RemoveAsync(account, CancellationToken);
            await dialog.ConfirmRemoveCommand.ExecuteAsync(null);
            await shown;
        }).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
        dialog.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_sync_folder_change_finishes_on_a_pool_thread_then_the_dialog_changes_on_the_ui_thread()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        folderPicker.Choice = Option.Some(fileSystem.DirectoryInfo.New("/data/sync"));
        actions.ChangeSyncRoot = async (_, candidate) => await Later(SyncRootFactory.Create(candidate));
        using var ui = new SingleThreadContext();
        var threads = Watch();

        await ui.RunAsync(async () =>
        {
            _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);
            await dialog.ChooseSyncFolderCommand.ExecuteAsync(null);
        }).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
        dialog.SyncFolderPathText.ShouldBe("/data/sync");
    }

    private static async Task<T> Later<T>(T value)
    {
        await Task.Delay(20).ConfigureAwait(false);

        return value;
    }

    private static async Task<T> Later<T>(Exception thrown)
    {
        await Task.Delay(20).ConfigureAwait(false);

        throw thrown;
    }

    private ConcurrentBag<int> Watch()
    {
        var threads = new ConcurrentBag<int>();
        dialog.PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        return threads;
    }
}
