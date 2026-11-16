using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using Avalonia;
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
    public async Task when_text_is_typed_in_the_command_search_box_then_only_matching_pair_tiles_and_account_cards_remain()
    {
        var window = await CreateShownWindowAsync();
        var searchBox = window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Name == "CommandSearchBox");

        searchBox.Text = "recipes";
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<Border>().Count(border => border.Classes.Contains("pair-tile")).ShouldBe(1);
        window.GetVisualDescendants().OfType<Border>().Count(border => border.Classes.Contains("account-card")).ShouldBe(1);
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_account_card_has_a_pause_or_resume_button_and_only_the_paused_account_reads_resume()
    {
        var window = await CreateShownWindowAsync();

        var texts = window.GetVisualDescendants().OfType<Button>().Select(button => button.Content).OfType<string>().Where(text => text is "Pause" or "Resume").ToList();

        texts.Count(text => text == "Pause").ShouldBe(3);
        texts.Count(text => text == "Resume").ShouldBe(1);
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_account_card_has_a_sync_now_or_cancel_button()
    {
        var window = await CreateShownWindowAsync();

        var texts = window.GetVisualDescendants().OfType<Button>().Where(button => button.DataContext is LayoutCAccountCardViewModel).Select(button => button.Content).OfType<string>().Where(text => text is "Sync now" or "Cancel").ToList();

        texts.Count(text => text == "Sync now").ShouldBe(3);
        texts.Count(text => text == "Cancel").ShouldBe(1);
    }

    [AvaloniaFact]
    public async Task when_remove_is_clicked_on_a_tile_then_the_question_shows_and_cancelling_hides_it()
    {
        var window = await CreateShownWindowAsync();
        var tile = window.GetVisualDescendants().OfType<Border>().First(border => border.Classes.Contains("pair-tile"));
        var pair = tile.DataContext.ShouldBeOfType<AStarDev.CloudSync.ViewModels.SyncPairs.SyncPairViewModel>();
        var question = tile.GetVisualDescendants().OfType<Border>().Single(border => border.Classes.Contains("remove-question"));
        var before = question.IsEffectivelyVisible;

        tile.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Remove")).Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var asked = question.IsEffectivelyVisible;
        pair.CancelRemoveCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        (before, asked, question.IsEffectivelyVisible).ShouldBe((false, true, false));
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

    [AvaloniaFact]
    public async Task when_shown_then_each_pair_tile_has_a_change_direction_button_bound_to_its_command()
    {
        var window = await CreateShownWindowAsync();

        var tiles = window.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("pair-tile")).ToList();

        tiles.ShouldNotBeEmpty();
        tiles.ShouldAllBe(tile => tile.GetVisualDescendants().OfType<Button>().Any(button => Equals(button.Content, "Change direction") && button.Command == ((SyncPairViewModel)tile.DataContext!).ChangeDirectionCommand));
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_new_sync_pair_and_add_account_buttons_are_bound_to_their_commands()
    {
        var window = await CreateShownWindowAsync();
        var layout = window.Content.ShouldBeOfType<LayoutCView>().DataContext.ShouldBeOfType<LayoutCViewModel>();
        var buttons = window.GetVisualDescendants().OfType<Button>().ToList();

        buttons.Single(button => Equals(button.Content, "+ New sync pair")).Command.ShouldBeSameAs(layout.NewSyncPairCommand);
        buttons.Single(button => Equals(button.Content, "+ Add account")).Command.ShouldBeSameAs(layout.AddAccountCommand);
    }

    private static List<Button> FindChips(Window window) => [.. window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("chip"))];

    [AvaloniaFact]
    public async Task when_no_account_is_selected_then_no_account_card_shows_action_buttons_and_selecting_one_shows_only_its_buttons()
    {
        var window = await CreateShownWindowAsync();
        var first = window.GetVisualDescendants().OfType<Button>().First(button => button.Classes.Contains("account-select"));
        var before = VisibleCardActions(window).Count;

        first.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var visibleActions = VisibleCardActions(window);
        before.ShouldBe(0);
        visibleActions.Count.ShouldBe(4);
        visibleActions.ShouldAllBe(button => ReferenceEquals(button.DataContext, first.DataContext));
    }

    [AvaloniaFact]
    public async Task when_another_account_card_is_selected_then_the_action_buttons_move_to_it()
    {
        var window = await CreateShownWindowAsync();
        var buttons = window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("account-select")).ToList();
        buttons[0].Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        var other = buttons[1];

        other.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var visibleActions = VisibleCardActions(window);
        visibleActions.Count.ShouldBe(4);
        visibleActions.ShouldAllBe(button => ReferenceEquals(button.DataContext, other.DataContext));
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_pair_tile_button_sits_inside_its_tile()
    {
        var window = await CreateShownWindowAsync();
        var tiles = window.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("pair-tile")).ToList();

        var overflowing = tiles.SelectMany(tile => tile.GetVisualDescendants().OfType<Button>().Where(button => button.IsEffectivelyVisible && button.GetVisualAncestors().OfType<Border>().All(ancestor => !ancestor.Classes.Contains("remove-question"))).Where(button => button.TranslatePoint(new Point(0, button.Bounds.Height), tile) is { } bottom && bottom.Y > tile.Bounds.Height)).ToList();

        tiles.ShouldNotBeEmpty();
        overflowing.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public async Task when_the_command_search_box_has_focus_then_it_draws_no_border_around_the_typed_text()
    {
        var window = await CreateShownWindowAsync();
        var searchBox = window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Name == "CommandSearchBox");

        searchBox.Focus();
        searchBox.Text = "f";
        Dispatcher.UIThread.RunJobs();

        var border = searchBox.GetVisualDescendants().OfType<Border>().Single(element => element.Name == "PART_BorderElement");
        searchBox.IsFocused.ShouldBeTrue();
        border.BorderThickness.ShouldBe(default);
    }

    private static List<Button> VisibleCardActions(Window window) =>
        [.. window.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("secondary") && button.DataContext is LayoutCAccountCardViewModel && button.IsEffectivelyVisible)];

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutCView { DataContext = new LayoutCViewModel(overview, new RecordingSyncActions(), TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
