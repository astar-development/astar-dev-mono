using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Classification;

public class GivenTheClassificationSuggestionsContent
{
    private static async Task<(Window Window, ClassificationSuggestionsViewModel List)> ShownAsync(int count)
    {
        var suggestions = Enumerable.Range(0, count).Select(index => SuggestionTestData.Plain($"word{index}", count - index)).ToArray();
        var categories = new FakeCategoryStore(ClassificationTestData.Category(ClassificationTestData.NewCategoryId(), "Colour"));
        var list = new ClassificationSuggestionsViewModel(new FakeClassificationSuggestions(suggestions), categories, TestData.CreateLocalization());
        await list.LoadCommand.ExecuteAsync(null);
        var window = new Window { Width = 900, Height = 600, Content = new ClassificationSuggestionsContent { DataContext = list } };
        window.Show();
        window.UpdateLayout();

        return (window, list);
    }

    [AvaloniaFact]
    public async Task when_eight_thousand_suggestions_are_shown_then_only_the_visible_rows_are_built()
    {
        var (window, _) = await ShownAsync(8000);

        window.GetVisualDescendants().OfType<Button>().Count().ShouldBeLessThan(500);
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_scroll_viewer_is_bounded_by_the_window()
    {
        var (window, _) = await ShownAsync(8000);

        var scroll = window.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Name == "SuggestionScroll");
        (scroll.Extent.Height > scroll.Viewport.Height, scroll.Viewport.Height > 0 && scroll.Viewport.Height < 600).ShouldBe((true, true));
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_interactive_control_has_a_screen_reader_name()
    {
        var (window, list) = await ShownAsync(3);
        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);
        window.UpdateLayout();

        var unnamed = window.GetVisualDescendants().OfType<Control>().Where(control => control is TextBox or Button).Where(control => string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(control)) && control is not Button { Content: string { Length: > 0 } }).ToList();

        unnamed.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public async Task when_the_picker_is_open_then_the_filter_and_category_buttons_are_shown_and_hidden_again_on_close()
    {
        var (window, list) = await ShownAsync(3);
        var closedBefore = window.GetVisualDescendants().OfType<TextBox>().Count(box => box.Name == "CategoryFilterBox" && box.IsEffectivelyVisible);

        await list.PromoteCommand.ExecuteAsync(list.Rows[0]);
        window.UpdateLayout();
        var open = window.GetVisualDescendants().OfType<TextBox>().Count(box => box.Name == "CategoryFilterBox" && box.IsEffectivelyVisible);
        list.ClosePickerCommand.Execute(null);
        window.UpdateLayout();
        var closedAfter = window.GetVisualDescendants().OfType<TextBox>().Count(box => box.Name == "CategoryFilterBox" && box.IsEffectivelyVisible);

        (closedBefore, open, closedAfter).ShouldBe((0, 1, 0));
    }

    [AvaloniaFact]
    public async Task when_a_row_is_promoted_through_its_button_then_the_picker_opens_for_that_word()
    {
        var (window, list) = await ShownAsync(3);

        var promote = window.GetVisualDescendants().OfType<Button>().First(button => button.Name == "PromoteButton");
        promote.Command!.Execute(promote.CommandParameter);
        window.UpdateLayout();

        (list.IsPickerOpen, list.PickerTitleText).ShouldBe((true, "Promote \"word0\" to a category"));
    }
}
