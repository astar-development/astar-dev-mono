using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenActivityViewDisplay
{
    private static ActivityViewModel CreateViewModel()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        var syncRepository = Substitute.For<ISyncRepository>();
        var syncEventAggregator = Substitute.For<ISyncEventAggregator>();
        var conflictFactory = Substitute.For<IConflictItemViewModelFactory>();
        var activityFactory = Substitute.For<IActivityItemViewModelFactory>();
        var dispatcher = Substitute.For<IUiDispatcher>();

        return new ActivityViewModel(syncRepository, syncEventAggregator, conflictFactory, activityFactory, dispatcher, localization);
    }

    private static ActivityView CreateViewWithViewModel(ActivityViewModel viewModel)
    {
        var view = new ActivityView { DataContext = viewModel };
        view.Measure(new(1000, 800));
        view.Arrange(new(0, 0, 1000, 800));

        return view;
    }

    private static Grid FindTabContentGrid(ActivityView view, ActivityViewModel viewModel, bool logTab)
    {
        return view.GetLogicalDescendants().OfType<Grid>().First(g =>
        {
            var itemsControls = g.GetLogicalDescendants().OfType<ItemsControl>().ToList();
            bool hasLog = itemsControls.Any(ic => ReferenceEquals(ic.ItemsSource, viewModel.FilteredLog));
            bool hasConflicts = itemsControls.Any(ic => ReferenceEquals(ic.ItemsSource, viewModel.Conflicts));

            return logTab ? hasLog && !hasConflicts : hasConflicts && !hasLog;
        });
    }

    [AvaloniaFact]
    public void when_view_is_first_shown_then_log_tab_content_is_visible()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var logGrid = FindTabContentGrid(sut, viewModel, logTab: true);
        logGrid.IsVisible.ShouldBeTrue("Log tab content should be visible because ActiveTab defaults to Log");
    }

    [AvaloniaFact]
    public void when_view_is_first_shown_then_conflicts_tab_content_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var conflictsGrid = FindTabContentGrid(sut, viewModel, logTab: false);
        conflictsGrid.IsVisible.ShouldBeFalse("Conflicts tab content should be hidden while the log tab is active");
    }

    [AvaloniaFact]
    public void when_active_tab_switches_to_conflicts_after_render_then_conflicts_content_becomes_visible()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.ActiveTab = ActivityTab.Conflicts;

        FindTabContentGrid(sut, viewModel, logTab: false).IsVisible.ShouldBeTrue("Conflicts tab content should become visible when ActiveTab changes");
        FindTabContentGrid(sut, viewModel, logTab: true).IsVisible.ShouldBeFalse("Log tab content should hide when ActiveTab changes to Conflicts");
    }

    [AvaloniaFact]
    public void when_log_is_empty_then_no_activity_placeholder_is_visible()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var placeholder = sut.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(sp => sp.IsVisible && sp.Children.OfType<TextBlock>().Any(tb => tb.Text == viewModel.NoActivityYetText));
        placeholder.ShouldNotBeNull("No-activity placeholder should be visible when the log is empty");
    }

    [AvaloniaFact]
    public void when_log_is_empty_then_log_scroll_viewer_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var logScrollViewer = sut.GetLogicalDescendants().OfType<ScrollViewer>().First(sv => sv.GetLogicalDescendants().OfType<ItemsControl>().Any(ic => ReferenceEquals(ic.ItemsSource, viewModel.FilteredLog)));
        logScrollViewer.IsVisible.ShouldBeFalse("Log list ScrollViewer should be hidden when HasLogItems is false");
    }

    [AvaloniaFact]
    public void when_log_items_control_is_inspected_then_items_source_is_filtered_log()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var logItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.FilteredLog));
        logItemsControl.ShouldNotBeNull("Log ItemsControl should be bound to FilteredLog");
    }

    [AvaloniaFact]
    public void when_conflicts_items_control_is_inspected_then_items_source_is_conflicts_collection()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var conflictsItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.Conflicts));
        conflictsItemsControl.ShouldNotBeNull("Conflicts ItemsControl should be bound to the Conflicts collection");
    }

    [AvaloniaFact]
    public void when_there_are_no_conflicts_then_conflict_badge_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var badge = sut.GetLogicalDescendants().OfType<Border>().FirstOrDefault(b => b.Child is TextBlock badgeText && badgeText.Text == viewModel.ConflictBadgeText);
        badge.ShouldNotBeNull();
        badge.IsVisible.ShouldBeFalse("Conflict count badge should be hidden when HasConflicts is false");
    }

    [AvaloniaFact]
    public void when_there_are_no_conflicts_then_no_conflicts_placeholder_exists_in_conflicts_tab()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var placeholder = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == viewModel.NoConflictsText);
        placeholder.ShouldNotBeNull("No-conflicts placeholder TextBlock should be present");
    }

    [AvaloniaFact]
    public void when_log_tab_is_active_then_filter_chips_panel_is_visible()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var filterPanel = sut.GetLogicalDescendants().OfType<StackPanel>().First(sp => sp.GetLogicalDescendants().OfType<Button>().Any(b => b.Command == viewModel.ClearLogCommand));
        filterPanel.IsVisible.ShouldBeTrue("Filter chips should be visible on the log tab");
    }

    [AvaloniaFact]
    public void when_conflicts_tab_is_active_then_filter_chips_panel_is_hidden()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.ActiveTab = ActivityTab.Conflicts;

        var filterPanel = sut.GetLogicalDescendants().OfType<StackPanel>().First(sp => sp.GetLogicalDescendants().OfType<Button>().Any(b => b.Command == viewModel.ClearLogCommand));
        filterPanel.IsVisible.ShouldBeFalse("Filter chips only apply to the log tab and should hide on the conflicts tab");
    }
}
