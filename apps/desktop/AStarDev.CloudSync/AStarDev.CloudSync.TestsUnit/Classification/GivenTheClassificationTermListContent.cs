using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Classification;

public class GivenTheClassificationTermListContent
{
    private static async Task<Window> ShownAsync(int termCount)
    {
        var colours = ClassificationTestData.NewCategoryId();
        var terms = Enumerable.Range(0, termCount).Select(index => ClassificationTestData.Term($"term {index}", colours)).ToArray();
        var list = new ClassificationTermListViewModel(new FakeTermStore(terms), new FakeCategoryStore(ClassificationTestData.Category(colours, "Colour")), new FakeFileClassificationStore(), TestData.CreateLocalization());
        await list.LoadCommand.ExecuteAsync(null);
        var window = new Window { Width = 900, Height = 600, Content = new ClassificationTermListContent { DataContext = list } };
        window.Show();
        window.UpdateLayout();

        return window;
    }

    [AvaloniaFact]
    public async Task when_eight_thousand_terms_are_shown_then_only_the_visible_rows_are_built()
    {
        var window = await ShownAsync(8000);

        window.GetVisualDescendants().OfType<Button>().Count().ShouldBeLessThan(500);
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_scroll_viewer_is_bounded_by_the_window()
    {
        var window = await ShownAsync(8000);

        var scroll = window.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Name == "TermScroll");
        (scroll.Extent.Height > scroll.Viewport.Height, scroll.Viewport.Height > 0 && scroll.Viewport.Height < 600).ShouldBe((true, true));
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_interactive_control_has_a_screen_reader_name()
    {
        var window = await ShownAsync(3);

        var unnamed = window.GetVisualDescendants().OfType<Control>().Where(control => control is TextBox or Button).Where(control => string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(control)) && control is not Button { Content: string { Length: > 0 } }).ToList();

        unnamed.ShouldBeEmpty();
    }
}
