using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Classification;

public class GivenTheClassificationPageContent
{
    private static Window Shown(ClassificationPageViewModel page)
    {
        var window = new Window { Width = 900, Height = 600, Content = new ClassificationPageContent { DataContext = page } };
        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static Button ButtonNamed(Window window, string name) => window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == name);

    [AvaloniaFact]
    public void when_shown_then_the_title_and_the_reclassify_button_are_localised_and_the_cancel_button_is_hidden()
    {
        var page = TestData.CreateClassificationPage();

        var window = Shown(page);

        window.GetVisualDescendants().OfType<TextBlock>().ShouldContain(text => text.Text == "Classification");
        ButtonNamed(window, "ReclassifyButton").Content.ShouldBe("Reclassify");
        ButtonNamed(window, "ReclassifyButton").Command.ShouldBeSameAs(page.Reclassify.ReclassifyCommand);
        ButtonNamed(window, "CancelReclassifyButton").IsEffectivelyVisible.ShouldBeFalse();
        window.GetVisualDescendants().OfType<ProgressBar>().Single().IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public async Task when_a_run_is_in_progress_then_the_progress_the_status_and_the_cancel_button_are_visible_and_the_reclassify_button_is_disabled()
    {
        var reclassifier = new FakeClassificationReclassifier { Reports = [ReclassifyProgressFactory.Create(500, 1)], Gate = new TaskCompletionSource() };
        var page = TestData.CreateClassificationPage(reclassifier);
        var window = Shown(page);

        var running = page.Reclassify.ReclassifyCommand.ExecuteAsync(null);
        await reclassifier.Started.Task;
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ProgressBar>().Single().IsEffectivelyVisible.ShouldBeTrue();
        ButtonNamed(window, "CancelReclassifyButton").IsEffectivelyVisible.ShouldBeTrue();
        ButtonNamed(window, "ReclassifyButton").IsEffectivelyEnabled.ShouldBeFalse();
        window.GetVisualDescendants().OfType<TextBlock>().ShouldContain(text => text.Text == "Reclassifying: 500 files done");
        page.Reclassify.CancelCommand.Execute(null);
        await running;
    }

    [AvaloniaFact]
    public void when_shown_then_the_term_list_and_suggestions_tabs_sit_under_the_header()
    {
        var window = Shown(TestData.CreateClassificationPage());

        var tabs = window.GetVisualDescendants().OfType<ClassificationTabsContent>().Single();
        var header = ButtonNamed(window, "ReclassifyButton");

        header.TranslatePoint(new Point(0, 0), window)!.Value.Y.ShouldBeLessThan(tabs.TranslatePoint(new Point(0, 0), window)!.Value.Y);
    }
}
