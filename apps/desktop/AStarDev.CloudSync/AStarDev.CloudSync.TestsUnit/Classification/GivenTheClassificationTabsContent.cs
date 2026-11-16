using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Classification;

public class GivenTheClassificationTabsContent
{
    private static Window Shown(ClassificationTabsViewModel tabs)
    {
        var window = new Window { Width = 900, Height = 600, Content = new ClassificationTabsContent { DataContext = tabs } };
        window.Show();
        window.UpdateLayout();

        return window;
    }

    private static ClassificationTabsViewModel Create()
    {
        var localization = TestData.CreateLocalization();
        var categories = new FakeCategoryStore(ClassificationTestData.Category(ClassificationTestData.NewCategoryId(), "Colour"));
        var terms = new ClassificationTermListViewModel(new FakeTermStore(), categories, new FakeFileClassificationStore(), localization);
        var suggestions = new ClassificationSuggestionsViewModel(new FakeClassificationSuggestions(SuggestionTestData.Plain("sunset", 4)), categories, localization);

        return new ClassificationTabsViewModel(terms, suggestions, localization);
    }

    private static (bool Terms, bool Suggestions) Visible(Window window) =>
        (window.GetVisualDescendants().OfType<ClassificationTermListContent>().Single().IsEffectivelyVisible, window.GetVisualDescendants().OfType<ClassificationSuggestionsContent>().Single().IsEffectivelyVisible);

    [AvaloniaFact]
    public void when_shown_then_the_terms_tab_is_visible_and_the_suggestions_tab_is_hidden()
    {
        var window = Shown(Create());

        Visible(window).ShouldBe((true, false));
    }

    [AvaloniaFact]
    public async Task when_the_suggestions_tab_header_is_pressed_then_the_suggestions_are_visible_and_the_terms_are_hidden()
    {
        var tabs = Create();
        var window = Shown(tabs);

        var header = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "SuggestionsTab");
        await tabs.ShowSuggestionsCommand.ExecuteAsync(null);
        window.UpdateLayout();

        (header.Command, Visible(window)).ShouldBe((tabs.ShowSuggestionsCommand, (false, true)));
    }

    [AvaloniaFact]
    public void when_shown_then_both_tab_headers_have_a_screen_reader_name()
    {
        var window = Shown(Create());

        var names = window.GetVisualDescendants().OfType<Button>().Where(button => button.Name is "TermsTab" or "SuggestionsTab").Select(button => Avalonia.Automation.AutomationProperties.GetName(button)).ToList();

        string.Join(',', names).ShouldBe("Terms,Suggestions");
    }
}
