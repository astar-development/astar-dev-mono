using AStarDev.CloudSync.Conflicts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.FunctionalParadigm;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Conflicts;

public class GivenAConflictListView
{
    [AvaloniaFact]
    public async Task when_shown_then_the_list_scrolls_inside_a_star_row_below_the_header()
    {
        var (window, _) = await ShowAsync(40, 400);

        var scroll = window.GetVisualDescendants().OfType<ScrollViewer>().First(candidate => candidate.Name == "ConflictScroll");
        var grid = scroll.GetVisualParent().ShouldBeOfType<Grid>();
        grid.RowDefinitions.Select(row => row.Height.GridUnitType).ShouldBe([GridUnitType.Auto, GridUnitType.Star]);
        Grid.GetRow(scroll).ShouldBe(1);
        scroll.MinHeight.ShouldBe(0);
        grid.HorizontalAlignment.ShouldBe(Avalonia.Layout.HorizontalAlignment.Stretch);
        grid.VerticalAlignment.ShouldBe(Avalonia.Layout.VerticalAlignment.Stretch);
        scroll.Extent.Height.ShouldBeGreaterThan(scroll.Viewport.Height);
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_root_stretches_its_content()
    {
        var (window, _) = await ShowAsync(1, 800);

        var view = window.GetVisualDescendants().OfType<ConflictListView>().Single();

        view.VerticalContentAlignment.ShouldBe(Avalonia.Layout.VerticalAlignment.Stretch);
        view.HorizontalContentAlignment.ShouldBe(Avalonia.Layout.HorizontalAlignment.Stretch);
    }

    [AvaloniaFact]
    public async Task when_shown_then_each_strategy_button_is_bound_to_its_command_and_has_an_automation_name_and_is_keyboard_reachable()
    {
        var (window, list) = await ShowAsync(2, 800);

        var item = list.Groups.Single().Items[0];
        var buttons = window.GetVisualDescendants().OfType<Button>().Where(button => item.Options.Any(option => option.Command == button.Command)).ToList();

        buttons.Count.ShouldBe(4);
        foreach (var option in item.Options)
        {
            var button = buttons.Single(candidate => candidate.Command == option.Command);
            AutomationProperties.GetName(button).ShouldBe(option.AutomationName);
            button.Content.ShouldBe(option.Text);
            button.Focusable.ShouldBeTrue();
            button.IsTabStop.ShouldBeTrue();
        }
    }

    [AvaloniaFact]
    public async Task when_a_strategy_does_not_apply_then_its_button_is_disabled_and_carries_the_reason_as_its_tooltip()
    {
        var (window, list) = await ShowAsync(0, 800, AttentionItems.CreateConflict(Account.Id, "a.jpg", ConflictKindFactory.CreateBothModified(), SideSnapshotFactory.CreateAbsent(), AttentionItems.Present()));

        var option = list.Groups.Single().Items.Single().Options[0];
        var button = window.GetVisualDescendants().OfType<Button>().Single(candidate => candidate.Command == option.Command);

        button.IsEffectivelyEnabled.ShouldBeFalse();
        ToolTip.GetTip(button).ShouldBe(option.DisabledReasonText);
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_filter_the_refresh_and_the_close_controls_are_named_and_bound()
    {
        var (window, list) = await ShowAsync(1, 800);

        var filter = window.GetVisualDescendants().OfType<ComboBox>().Single();
        var buttons = window.GetVisualDescendants().OfType<Button>().ToList();

        AutomationProperties.GetName(filter).ShouldBe(list.FilterLabelText);
        filter.ItemsSource.ShouldBe(list.Filters);
        filter.SelectedItem.ShouldBeSameAs(list.SelectedFilter);
        buttons.Single(button => button.Command == list.RefreshCommand).Content.ShouldBe("Refresh");
        buttons.Single(button => button.Command == list.CloseCommand).Content.ShouldBe("Close");
        AutomationProperties.GetName(buttons.Single(button => button.Command == list.CloseCommand)).ShouldBe("Close");
    }

    [AvaloniaFact]
    public async Task when_the_queue_is_empty_then_the_empty_text_shows()
    {
        var (window, list) = await ShowAsync(0, 800);

        window.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text).ShouldContain(list.EmptyText);
    }

    [AvaloniaFact]
    public async Task when_a_conflict_has_an_error_then_the_error_text_shows_on_it()
    {
        var resolver = new FakeConflictResolver { Outcome = (_, _, _, _) => Task.FromResult(Result.Failure<ResolutionSummary, ErrorResponse>(new ErrorResponse("busy"))) };
        var (window, list) = await ShowAsync(0, 800, resolver, AttentionItems.CreateConflict(Account.Id, "a.jpg"));
        var item = list.Groups.Single().Items.Single();

        await item.Options[0].Command.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();

        item.HasError.ShouldBeTrue();
        window.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text).ShouldContain(item.ErrorText);
    }

    private static readonly CloudAccount Account = AttentionItems.Account("Photos");

    private static Task<(Window Window, ConflictListViewModel List)> ShowAsync(int generated, double height, params SyncAttentionItem[] extra) => ShowAsync(generated, height, new FakeConflictResolver(), extra);

    private static async Task<(Window Window, ConflictListViewModel List)> ShowAsync(int generated, double height, FakeConflictResolver resolver, params SyncAttentionItem[] extra)
    {
        var reader = new FakeAttentionReader();
        reader.Add(Account.Id, [.. Enumerable.Range(0, generated).Select(index => AttentionItems.CreateConflict(Account.Id, $"Camera/{index}.jpg")), .. extra]);
        var list = TestData.CreateConflictList(new FixedAccountsSource(Account), reader, resolver);
        await list.LoadAsync(TestContext.Current.CancellationToken);
        var window = new Window { Width = 1000, Height = height, Content = new ConflictListView { DataContext = list } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return (window, list);
    }
}
