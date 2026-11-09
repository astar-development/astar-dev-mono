using AStarDev.CloudSync.Layouts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Layouts;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Layouts;

public class GivenALayoutBView
{
    [AvaloniaFact]
    public async Task when_shown_then_the_selected_account_and_its_folders_are_rendered()
    {
        var window = await CreateShownWindowAsync();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToList();

        texts.ShouldContain("OneDrive / Work");
        texts.ShouldContain("Documents");
        texts.ShouldNotContain("Camera");
    }

    [AvaloniaFact]
    public async Task when_shown_then_each_local_path_is_also_its_tooltip()
    {
        var window = await CreateShownWindowAsync();

        var localPath = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Documents");

        ToolTip.GetTip(localPath).ShouldBe("Documents");
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_four_tabs_are_present_with_overview_selected()
    {
        var window = await CreateShownWindowAsync();

        var tabs = window.GetVisualDescendants().OfType<TabControl>().ShouldHaveSingleItem();

        tabs.ItemCount.ShouldBe(4);
        tabs.SelectedIndex.ShouldBe(0);
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_scroll_viewer_has_a_bounded_viewport()
    {
        var window = await CreateShownWindowAsync();

        var scrollViewers = window.GetVisualDescendants().OfType<ScrollViewer>().ToList();

        scrollViewers.ShouldNotBeEmpty();
        scrollViewers.ShouldAllBe(scrollViewer => scrollViewer.Viewport.Height <= window.Height);
    }

    [AvaloniaFact]
    public async Task when_text_is_typed_in_the_search_box_then_the_tree_and_folders_are_filtered()
    {
        var window = await CreateShownWindowAsync();
        var searchBox = window.GetVisualDescendants().OfType<TextBox>().ShouldHaveSingleItem();

        searchBox.Text = "holiday";
        Dispatcher.UIThread.RunJobs();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToList();
        window.Content.ShouldBeOfType<LayoutBView>().DataContext.ShouldBeOfType<LayoutBViewModel>().SearchText.ShouldBe("holiday");
        texts.ShouldContain("OneDrive / Photos");
        texts.ShouldContain("Camera");
        texts.ShouldNotContain("Documents");
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_connect_pause_sync_now_and_add_folder_buttons_are_bound_to_their_commands()
    {
        var window = await CreateShownWindowAsync();
        var layout = window.Content.ShouldBeOfType<LayoutBView>().DataContext.ShouldBeOfType<LayoutBViewModel>();
        var detail = layout.Detail.ShouldBeOfType<AccountDetailViewModel>();
        var buttons = window.GetVisualDescendants().OfType<Button>().ToList();

        buttons.Single(button => Equals(button.Content, "+ Connect a provider")).Command.ShouldBeSameAs(layout.ConnectProviderCommand);
        buttons.Single(button => Equals(button.Content, "Pause")).Command.ShouldBeSameAs(detail.PauseCommand);
        buttons.Single(button => Equals(button.Content, "Sync now")).Command.ShouldBeSameAs(detail.SyncNowCommand);
        buttons.Single(button => Equals(button.Content, "+ Add folder")).Command.ShouldBeSameAs(detail.AddFolderCommand);
    }

    private static async Task<Window> CreateShownWindowAsync()
    {
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var window = new Window { Width = 1280, Height = 800, Content = new LayoutBView { DataContext = new LayoutBViewModel(overview, TestData.CreateLocalPathFormatter(), new RecordingSyncActions(), TestData.CreateLocalization()) } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
