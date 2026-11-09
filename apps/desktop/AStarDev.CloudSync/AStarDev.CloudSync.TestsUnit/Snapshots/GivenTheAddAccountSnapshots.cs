using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheAddAccountSnapshots
{
    [AvaloniaTheory]
    [InlineData("signing-in", "dark")]
    [InlineData("signing-in", "light")]
    [InlineData("consent", "dark")]
    [InlineData("consent", "light")]
    [InlineData("already-added", "dark")]
    [InlineData("already-added", "light")]
    [InlineData("failed", "dark")]
    [InlineData("failed", "light")]
    [InlineData("offer", "dark")]
    [InlineData("offer", "light")]
    public void when_rendered_then_the_add_account_dialog_matches_the_baseline(string step, string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var dialog = CreateDialogAt(step);
        var window = new Window
        {
            Content = new Border
            {
                Padding = new Avalonia.Thickness(16),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("CloudSyncBackgroundBrush"),
                Child = new AddAccountView { DataContext = dialog }
            }
        };

        Snapshot.ShouldMatchBaseline(window, theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light, $"control-add-account-{step}-{theme}", 520, 320);
    }

    private static AddAccountViewModel CreateDialogAt(string step)
    {
        var consent = new TokenCacheConsentViewModel(new ImmediateUiDispatcher(), TestData.CreateLocalization());
        var adder = new FakeAccountAdder
        {
            Add = step switch
            {
                "consent" => async token => { _ = await consent.AskAsync(token); return AccountAddOutcomeFactory.CreateCancelled(); },
                "already-added" => _ => Task.FromResult(AccountAddOutcomeFactory.CreateReAuthenticated(AccountIdFactory.Create(Guid.Parse("5e6f7a8b-9c0d-4e1f-a2b3-c4d5e6f7a8b9")))),
                "failed" => _ => Task.FromResult(AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork())),
                "offer" => _ => Task.FromResult(AccountAddOutcomeFactory.CreateAdded(AccountIdFactory.Create(Guid.Parse("5e6f7a8b-9c0d-4e1f-a2b3-c4d5e6f7a8b9")))),
                _ => _ => new TaskCompletionSource<AccountAddOutcome>().Task,
            }
        };
        var dialog = new AddAccountViewModel(new SingleProviderCapability<IAccountAdder>(adder), consent, new RecordingFolderSelectionDialog(), TestData.CreateLocalization(), NullLogger<AddAccountViewModel>.Instance);
        _ = dialog.ShowAsync(TestContext.Current.CancellationToken);

        return dialog;
    }
}
