using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheAccountDialogSnapshots
{
    [AvaloniaTheory]
    [InlineData("confirm", "dark")]
    [InlineData("confirm", "light")]
    [InlineData("failed", "dark")]
    [InlineData("failed", "light")]
    public async Task when_rendered_then_the_account_dialog_matches_the_baseline(string step, string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var actions = new FakeAccountActions { Remove = _ => Task.FromResult(Result.Failure<Unit, AccountRemovalFailure>(AccountRemovalFailureFactory.CreateSyncStillRunning())) };
        var dialog = TestData.CreateAccountDialog(actions);
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), dialog);
        _ = dialog.RemoveAsync(account, TestContext.Current.CancellationToken);
        if (step == "failed")
            await dialog.ConfirmRemoveCommand.ExecuteAsync(null);
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncBackgroundBrush"),
                Child = new AccountDialogView { DataContext = dialog }
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-account-dialog-{step}-{theme}", 520, 280);
    }
}
