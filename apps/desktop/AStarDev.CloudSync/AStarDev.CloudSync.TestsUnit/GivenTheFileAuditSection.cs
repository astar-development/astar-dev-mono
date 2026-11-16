using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenTheFileAuditSection
{
    private readonly FakeFileAuditor auditor = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static IEnumerable<string> ShownTexts(Window window) => window.GetVisualDescendants().OfType<TextBlock>().Where(block => block.IsEffectivelyVisible).Select(block => block.Text ?? string.Empty);

    private static IEnumerable<string> ShownButtons(Window window) => window.GetVisualDescendants().OfType<Button>().Where(button => button.IsEffectivelyVisible).Select(button => button.Content?.ToString() ?? string.Empty);

    private async Task<(FileAuditViewModel Audit, Window Window)> ShownAsync()
    {
        var audit = new FileAuditViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), auditor, TestData.CreateLocalization());
        var settings = TestData.CreateDisplaySettings(audit);
        var window = new Window { Content = new DisplaySettingsView { DataContext = settings } };
        window.Show();

        return (audit, window);
    }

    [AvaloniaFact]
    public async Task when_an_account_is_chosen_then_the_audit_button_shows_and_the_warning_does_not()
    {
        var (audit, window) = await ShownAsync();

        audit.Accounts[0].SelectCommand.Execute(null);
        window.UpdateLayout();

        ShownButtons(window).ShouldContain("Audit");
        ShownTexts(window).ShouldNotContain(audit.WarningText);
    }

    [AvaloniaFact]
    public async Task when_audit_is_pressed_then_the_slow_warning_shows_with_run_and_cancel_in_place_of_the_audit_button()
    {
        var (audit, window) = await ShownAsync();
        audit.Accounts[0].SelectCommand.Execute(null);

        audit.AuditCommand.Execute(null);
        window.UpdateLayout();

        ShownTexts(window).ShouldContain(audit.WarningText);
        ShownButtons(window).ShouldContain("Run audit");
        ShownButtons(window).ShouldContain("Cancel");
        ShownButtons(window).ShouldNotContain("Audit");
    }

    [AvaloniaFact]
    public async Task when_the_audit_finishes_then_both_counts_show()
    {
        var (audit, window) = await ShownAsync();
        audit.Accounts[0].SelectCommand.Execute(null);
        audit.AuditCommand.Execute(null);

        await audit.ConfirmCommand.ExecuteAsync(null);
        window.UpdateLayout();

        ShownTexts(window).ShouldContain("Local files: 1,234");
        ShownTexts(window).ShouldContain("Cloud files: 5,678");
    }
}
