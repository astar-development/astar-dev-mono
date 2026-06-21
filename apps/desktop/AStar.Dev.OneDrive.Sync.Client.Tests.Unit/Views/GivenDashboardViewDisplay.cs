using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenDashboardViewDisplay
{
    private static DashboardView CreateViewWithViewModel(DashboardViewModel viewModel)
    {
        var view = new DashboardView { DataContext = viewModel };
        view.Measure(new(1000, 800));
        view.Arrange(new(0, 0, 1000, 800));
        return view;
    }

    private static DashboardViewModel CreateViewModelWithNoAccounts()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns("Label");
        var syncAggregator = Substitute.For<ISyncEventAggregator>();
        var accountFactory = Substitute.For<IDashboardAccountViewModelFactory>();
        var activityFactory = Substitute.For<IActivityItemViewModelFactory>();

        return new DashboardViewModel(localization, syncAggregator, accountFactory, activityFactory, Substitute.For<IUiTimer>());
    }

    [AvaloniaFact]
    public void when_dashboard_has_no_accounts_then_no_accounts_placeholder_is_visible()
    {
        var viewModel = CreateViewModelWithNoAccounts();

        var sut = CreateViewWithViewModel(viewModel);

        var noAccountsPanel = sut.GetLogicalDescendants()
            .OfType<StackPanel>()
            .FirstOrDefault(sp => sp.IsVisible && sp.Children
                .OfType<TextBlock>()
                .Count() >= 2);

        noAccountsPanel.ShouldNotBeNull("No-accounts placeholder should be visible when HasAccounts is false");
    }

    [AvaloniaFact]
    public void when_dashboard_has_accounts_then_no_accounts_placeholder_is_hidden()
    {
        var viewModel = CreateViewModelWithNoAccounts();
        viewModel.TotalAccounts = 1;

        var sut = CreateViewWithViewModel(viewModel);

        var noAccountsPanel = sut.GetLogicalDescendants()
            .OfType<StackPanel>()
            .FirstOrDefault(sp => !sp.IsVisible && sp.Children
                .OfType<TextBlock>()
                .Count() >= 2);

        noAccountsPanel.ShouldNotBeNull("No-accounts placeholder should be hidden when HasAccounts is true");
    }

    [AvaloniaFact]
    public void when_dashboard_has_accounts_then_account_sections_scroll_viewer_is_visible()
    {
        var viewModel = CreateViewModelWithNoAccounts();
        viewModel.TotalAccounts = 1;

        var sut = CreateViewWithViewModel(viewModel);

        var scrollViewer = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault(sv => sv.IsVisible &&
                sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto &&
                sv.MinHeight == 0);

        scrollViewer.ShouldNotBeNull("ScrollViewer for account sections should be visible and properly configured");
    }

    [AvaloniaFact]
    public void when_dashboard_view_is_instantiated_then_header_grid_with_stat_columns_is_present()
    {
        var viewModel = CreateViewModelWithNoAccounts();

        var sut = CreateViewWithViewModel(viewModel);

        var topBorder = sut.GetLogicalDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Child is Grid grid && grid.ColumnDefinitions.Count == 9);

        topBorder.ShouldNotBeNull("Header border with 9 columns (5 stats + 4 separators) should be present");
    }

    [AvaloniaFact]
    public void when_dashboard_header_inspected_then_stat_text_blocks_exist_for_accounts_folders_conflicts()
    {
        var viewModel = CreateViewModelWithNoAccounts();

        var sut = CreateViewWithViewModel(viewModel);

        var topBorder = sut.GetLogicalDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Child is Grid grid && grid.ColumnDefinitions.Count == 9);

        topBorder.ShouldNotBeNull();

        var statTextBlocks = topBorder!.GetLogicalDescendants()
            .OfType<TextBlock>()
            .Where(tb => !string.IsNullOrEmpty(tb.Text))
            .ToList();

        statTextBlocks.ShouldNotBeEmpty("Header should contain stat text blocks");
        statTextBlocks.Count.ShouldBeGreaterThanOrEqualTo(5, "Should have at least 5 stat labels");
    }

    [AvaloniaFact]
    public void when_dashboard_with_multiple_accounts_then_items_control_bound_to_account_sections()
    {
        var viewModel = CreateViewModelWithNoAccounts();
        viewModel.TotalAccounts = 2;

        var sut = CreateViewWithViewModel(viewModel);

        var itemsControl = sut.GetLogicalDescendants()
            .OfType<ItemsControl>()
            .FirstOrDefault();

        itemsControl.ShouldNotBeNull("ItemsControl for account sections should be present");
        itemsControl!.ItemsSource.ShouldBe(viewModel.AccountSections);
    }

    [AvaloniaFact]
    public void when_dashboard_view_is_rendered_then_root_grid_has_two_rows()
    {
        var viewModel = CreateViewModelWithNoAccounts();

        var sut = CreateViewWithViewModel(viewModel);

        var grids = sut.GetLogicalDescendants()
            .OfType<Grid>()
            .Where(g => g.RowDefinitions.Count == 2)
            .ToList();

        grids.ShouldNotBeEmpty("Root grid with Auto (header) and Star (content) rows should be present");
    }

    [AvaloniaFact]
    public void when_dashboard_has_no_accounts_then_account_sections_scroll_viewer_is_hidden()
    {
        var viewModel = CreateViewModelWithNoAccounts();

        var sut = CreateViewWithViewModel(viewModel);

        var scrollViewer = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault(sv => sv.VerticalScrollBarVisibility == ScrollBarVisibility.Auto && sv.MinHeight == 0);

        scrollViewer.ShouldNotBeNull();
        scrollViewer.IsVisible.ShouldBeFalse("Account sections ScrollViewer should be hidden when no accounts exist");
    }

    [AvaloniaFact]
    public void when_total_accounts_is_set_then_header_displays_account_count()
    {
        var viewModel = CreateViewModelWithNoAccounts();
        viewModel.TotalAccounts = 3;

        var sut = CreateViewWithViewModel(viewModel);

        var countTextBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "3");
        countTextBlock.ShouldNotBeNull("Header should display the total account count");
    }

    [AvaloniaFact]
    public void when_total_folders_is_set_then_header_displays_folder_count()
    {
        var viewModel = CreateViewModelWithNoAccounts();
        viewModel.TotalFolders = 42;

        var sut = CreateViewWithViewModel(viewModel);

        var countTextBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "42");
        countTextBlock.ShouldNotBeNull("Header should display the total folder count");
    }

    [AvaloniaFact]
    public void when_total_conflicts_is_set_then_header_displays_conflict_count()
    {
        var viewModel = CreateViewModelWithNoAccounts();
        viewModel.TotalConflicts = 7;

        var sut = CreateViewWithViewModel(viewModel);

        var countTextBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "7");
        countTextBlock.ShouldNotBeNull("Header should display the total conflict count");
    }
}
