using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenFilesViewDisplay
{
    private static FilesView CreateViewWithViewModel(FilesViewModel viewModel)
    {
        var view = new FilesView { DataContext = viewModel };
        view.Measure(new(1000, 800));
        view.Arrange(new(0, 0, 1000, 800));
        return view;
    }

    private static FilesViewModel CreateViewModel()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        var accountFilesFactory = Substitute.For<IAccountFilesViewModelFactory>();

        return new FilesViewModel(accountFilesFactory, localization);
    }

    [AvaloniaFact]
    public void when_view_has_no_tabs_then_no_accounts_placeholder_is_visible()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var placeholder = sut.GetLogicalDescendants()
            .OfType<StackPanel>()
            .FirstOrDefault(sp => sp.IsVisible && sp.Children
                .OfType<TextBlock>()
                .Count() >= 2);

        placeholder.ShouldNotBeNull("No-accounts placeholder should be visible when Tabs is empty");
    }

    [AvaloniaFact]
    public void when_view_has_no_tabs_then_placeholder_shows_localization_key_for_heading()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var headingBlock = sut.GetLogicalDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text == "Files.NoAccountsConnected");

        headingBlock.ShouldNotBeNull("Heading text block should echo localization key Files.NoAccountsConnected");
    }

    [AvaloniaFact]
    public void when_view_has_no_tabs_then_placeholder_shows_localization_key_for_hint()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var hintBlock = sut.GetLogicalDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(tb => tb.Text == "Files.NoAccountsConnectedHint");

        hintBlock.ShouldNotBeNull("Hint text block should echo localization key Files.NoAccountsConnectedHint");
    }

    [AvaloniaFact]
    public void when_view_has_no_tabs_then_active_tab_content_grid_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var contentGrid = sut.GetLogicalDescendants()
            .OfType<Grid>()
            .FirstOrDefault(g => !g.IsVisible && g.Children.OfType<ContentControl>().Any());

        contentGrid.ShouldNotBeNull("Active tab content grid should be hidden when HasTabs is false");
    }

    [AvaloniaFact]
    public void when_view_is_rendered_then_root_grid_has_two_rows()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var rootGrid = sut.GetLogicalDescendants()
            .OfType<Grid>()
            .FirstOrDefault(g => g.RowDefinitions.Count == 2);

        rootGrid.ShouldNotBeNull("Root grid with Auto (tab strip) and Star (content) rows should be present");
    }

    [AvaloniaFact]
    public void when_view_is_rendered_then_tab_strip_border_is_present()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var tabStripBorder = sut.GetLogicalDescendants()
            .OfType<Border>()
            .FirstOrDefault(b => b.Child is ScrollViewer sv && sv.Content is ItemsControl);

        tabStripBorder.ShouldNotBeNull("Tab strip border containing ScrollViewer > ItemsControl should be present");
    }

    [AvaloniaFact]
    public void when_view_is_rendered_then_tab_strip_items_control_is_bound_to_tabs_collection()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var tabStrip = sut.GetLogicalDescendants()
            .OfType<ItemsControl>()
            .FirstOrDefault(ic => ic.ItemsSource == viewModel.Tabs);

        tabStrip.ShouldNotBeNull("Tab strip ItemsControl should be bound to the Tabs collection");
    }

    [AvaloniaFact]
    public void when_view_is_rendered_then_tab_strip_scroll_viewer_has_horizontal_scrolling()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var tabScrollViewer = sut.GetLogicalDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault(sv => sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Auto
                && sv.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled);

        tabScrollViewer.ShouldNotBeNull("Tab strip ScrollViewer should scroll horizontally and disable vertical scrollbar");
    }

    [AvaloniaFact]
    public void when_has_no_accounts_is_true_then_placeholder_stack_panel_is_visible()
    {
        var viewModel = CreateViewModel();

        viewModel.HasNoAccounts.ShouldBeTrue("HasNoAccounts should be true when Tabs is empty");

        var sut = CreateViewWithViewModel(viewModel);

        var placeholder = sut.GetLogicalDescendants()
            .OfType<StackPanel>()
            .FirstOrDefault(sp => sp.IsVisible
                && sp.Children.OfType<TextBlock>().Any(tb => tb.Text == "Files.NoAccountsConnected"));

        placeholder.ShouldNotBeNull("Placeholder StackPanel should be visible when HasNoAccounts is true");
    }

    [AvaloniaFact]
    public void when_has_tabs_is_false_then_active_tab_content_is_not_visible()
    {
        var viewModel = CreateViewModel();

        viewModel.HasTabs.ShouldBeFalse("HasTabs should be false when Tabs is empty");

        var sut = CreateViewWithViewModel(viewModel);

        bool hiddenContentExists = sut.GetLogicalDescendants()
            .OfType<Grid>()
            .Where(g => !g.IsVisible)
            .SelectMany(g => g.GetLogicalDescendants().OfType<ContentControl>())
            .Any();

        hiddenContentExists.ShouldBeTrue("Content area should not be visible when HasTabs is false");
    }

    [AvaloniaFact]
    public void when_active_tab_content_grid_inspected_then_content_control_is_present()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var contentControl = sut.GetLogicalDescendants()
            .OfType<ContentControl>()
            .FirstOrDefault();

        contentControl.ShouldNotBeNull("ContentControl for active tab should be present in the logical tree");
    }
}
