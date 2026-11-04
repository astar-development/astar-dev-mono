using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.ViewModels;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Layouts;

public class GivenALayoutCView
{
    [AvaloniaFact]
    public async Task when_ctrl_k_is_pressed_then_the_command_search_box_has_focus()
    {
        var window = await CreateShownWindowAsync();
        var searchBox = window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Name == "CommandSearchBox");

        window.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();

        searchBox.IsFocused.ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_all_pairs_chip_is_highlighted_and_the_provider_chips_are_not()
    {
        var window = await CreateShownWindowAsync();

        var chips = FindChips(window);

        chips.Count.ShouldBe(2);
        chips[0].Classes.ShouldContain("selected");
        chips[1].Classes.ShouldNotContain("selected");
    }

    [AvaloniaFact]
    public async Task when_a_provider_chip_is_clicked_then_it_is_highlighted_and_the_all_pairs_chip_is_not()
    {
        var window = await CreateShownWindowAsync();
        var chips = FindChips(window);

        chips[1].Command.ShouldNotBeNull().Execute(chips[1].CommandParameter);
        Dispatcher.UIThread.RunJobs();

        chips[0].Classes.ShouldNotContain("selected");
        chips[1].Classes.ShouldContain("selected");
    }

    [AvaloniaFact]
    public async Task when_shown_then_each_pair_tile_shows_the_direction_glyph_with_the_direction_label_as_its_accessible_name()
    {
        var window = await CreateShownWindowAsync();

        var tiles = window.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("pair-tile")).ToList();

        tiles.ShouldNotBeEmpty();
        tiles.ShouldAllBe(tile => tile.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == ((SyncPairViewModel)tile.DataContext!).DirectionGlyph && AutomationProperties.GetName(text) == ((SyncPairViewModel)tile.DataContext!).DirectionLabel));
    }

    private static List<Button> FindChips(Window window) => [.. window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("chip"))];

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutCView { DataContext = new LayoutCViewModel(overview, TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
